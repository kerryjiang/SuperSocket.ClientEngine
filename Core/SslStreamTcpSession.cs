using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace SuperSocket.ClientEngine
{
    public class SslStreamTcpSession : TcpClientSession
    {
        class SslAsyncState
        {
            public SslStream SslStream { get; set; }

            public Socket Client { get; set; }

            public PosList<ArraySegment<byte>> SendingItems { get; set; }
        }

        private SslStream m_SslStream;

        public bool AllowUnstrustedCertificate { get; set; }

        public SslStreamTcpSession(EndPoint remoteEndPoint)
            : base(remoteEndPoint)
        {

        }

        public SslStreamTcpSession(EndPoint remoteEndPoint, int receiveBufferSize)
            : base(remoteEndPoint, receiveBufferSize)
        {

        }

        protected override void SocketEventArgsCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessConnect(sender as Socket, null, e);
        }

        protected override void OnGetSocket(SocketAsyncEventArgs e)
        {
            try
            {
#if !SILVERLIGHT
                var sslStream = new SslStream(new NetworkStream(Client), false, ValidateRemoteCertificate);
#else
                var sslStream = new SslStream(new NetworkStream(Client));
#endif

                sslStream.BeginAuthenticateAsClient(HostName, OnAuthenticated, sslStream);
            }
            catch (Exception exc)
            {
                if (!IsIgnorableException(exc))
                    OnError(exc);
            }
        }

        private void OnAuthenticated(IAsyncResult result)
        {
            var sslStream = result.AsyncState as SslStream;

            try
            {
                sslStream.EndAuthenticateAsClient(result);
            }
            catch(Exception e)
            {
                OnError(e);
                return;
            }

            m_SslStream = sslStream;

            OnConnected();

            if(Buffer.Array == null)
                Buffer = new ArraySegment<byte>(new byte[ReceiveBufferSize], 0, ReceiveBufferSize);

            BeginRead();
        }

        private void OnDataRead(IAsyncResult result)
        {
            var state = result.AsyncState as SslAsyncState;
            var sslStream = state.SslStream;

            int length = 0;

            try
            {
                length = sslStream.EndRead(result);
            }
            catch (Exception e) 
            {
                if (!IsIgnorableException(e))
                    OnError(e);

                if (EnsureSocketClosed(state.Client))
                    OnClosed();

                return;
            }

            if (length == 0)
            {
                if (EnsureSocketClosed(state.Client))
                    OnClosed();

                return;
            }

            OnDataReceived(Buffer.Array, Buffer.Offset, length);
            BeginRead();
        }

        void BeginRead()
        {
            var client = Client;

            if (client == null)
                return;

            try
            {
                m_SslStream.BeginRead(Buffer.Array, Buffer.Offset, Buffer.Count, OnDataRead, new SslAsyncState { SslStream = m_SslStream, Client = client });
            }
            catch (Exception e)
            {
                if (!IsIgnorableException(e))
                    OnError(e);

                if (EnsureSocketClosed(client))
                    OnClosed();
            }
        }

#if !SILVERLIGHT
        /// <summary>
        /// Validates the remote certificate.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="certificate">The certificate.</param>
        /// <param name="chain">The chain.</param>
        /// <param name="sslPolicyErrors">The SSL policy errors.</param>
        /// <returns></returns>
        private bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            if (!AllowUnstrustedCertificate)
            {
                OnError(new Exception(sslPolicyErrors.ToString()));
                return false;
            }

#if DEBUG
            //In debug mode, ignore certificate name mismatch error
            if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) == SslPolicyErrors.RemoteCertificateNameMismatch)
            {
                return true;
            }
#endif

            //Not a remote certificate error
            if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) == 0)
            {
                OnError(new Exception(sslPolicyErrors.ToString()));
                return false;
            }

            if (chain != null && chain.ChainStatus != null)
            {
                foreach (X509ChainStatus status in chain.ChainStatus)
                {
                    if ((certificate.Subject == certificate.Issuer) &&
                       (status.Status == X509ChainStatusFlags.UntrustedRoot))
                    {
                        // Self-signed certificates with an untrusted root are valid. 
                        continue;
                    }
                    else
                    {
                        if (status.Status != X509ChainStatusFlags.NoError)
                        {
                            OnError(new Exception(sslPolicyErrors.ToString()));
                            // If there are any other errors in the certificate chain, the certificate is invalid,
                            // so the method returns false.
                            return false;
                        }
                    }
                }
            }

            // When processing reaches this line, the only errors in the certificate chain are 
            // untrusted root errors for self-signed certificates. These certificates are valid
            // for default Exchange server installations, so return true.
            return true;
        }

#endif
        protected override bool IsIgnorableException(Exception e)
        {
            if (base.IsIgnorableException(e))
                return true;

            if (e is System.IO.IOException)
            {
                if (e.InnerException is ObjectDisposedException)
                    return true;

                //In mono, some exception is wrapped like IOException -> IOException -> ObjectDisposedException
                if (e.InnerException is System.IO.IOException)
                {
                    if (e.InnerException.InnerException is ObjectDisposedException)
                        return true;
                }
            }

            return false;
        }

        protected override void SendInternal(PosList<ArraySegment<byte>> items)
        {
            var client = this.Client;

            try
            {
                var item = items[items.Position];
                m_SslStream.BeginWrite(item.Array, item.Offset, item.Count,
                    OnWriteComplete, new SslAsyncState { SslStream = m_SslStream, Client = client, SendingItems = items });
            }
            catch (Exception e)
            {
                if (!IsIgnorableException(e))
                    OnError(e);

                if (EnsureSocketClosed(client))
                    OnClosed();
            }
        }

        private void OnWriteComplete(IAsyncResult result)
        {
            var state = result.AsyncState as SslAsyncState;
            var sslStream = state.SslStream;

            try
            {
                sslStream.EndWrite(result);
            }
            catch (Exception e)
            {
                if (!IsIgnorableException(e))
                    OnError(e);

                if (EnsureSocketClosed(state.Client))
                    OnClosed();

                return;
            }

            var items = state.SendingItems;
            var nextPos = items.Position + 1;

            //Has more data to send
            if (nextPos < items.Count)
            {
                items.Position = nextPos;
                SendInternal(items);
                return;
            }

            try
            {
                m_SslStream.Flush();
            }
            catch (Exception e)
            {
                if (!IsIgnorableException(e))
                    OnError(e);

                if (EnsureSocketClosed(state.Client))
                    OnClosed();

                return;
            }

            OnSendingCompleted();
        }
    }
}
