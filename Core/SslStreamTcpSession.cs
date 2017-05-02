using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Threading;
#if NETSTANDARD
using System.Threading.Tasks;
#endif
#if !SILVERLIGHT
using System.Security.Authentication;
#endif
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
                

        public SslStreamTcpSession()
            : base()
        {

        }


#if !SILVERLIGHT

        public SecurityOption Security { get; set; }

#endif

        protected override void SocketEventArgsCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessConnect(sender as Socket, null, e, null);
        }

        protected override void OnGetSocket(SocketAsyncEventArgs e)
        {
            try
            {
#if SILVERLIGHT
                var sslStream = new SslStream(new NetworkStream(Client));
                sslStream.BeginAuthenticateAsClient(HostName, OnAuthenticated, sslStream);
#else
                var securityOption = Security;

                if (securityOption == null)
                {
                    throw new Exception("securityOption was not configured");
                }

#if NETSTANDARD

                AuthenticateAsClientAsync(new SslStream(new NetworkStream(Client), false, ValidateRemoteCertificate), Security);             
 
#else

                var sslStream = new SslStream(new NetworkStream(Client), false, ValidateRemoteCertificate);
                sslStream.BeginAuthenticateAsClient(HostName, securityOption.Certificates, securityOption.EnabledSslProtocols, false, OnAuthenticated, sslStream);
                
#endif
#endif

            }
            catch (Exception exc)
            {
                if (!IsIgnorableException(exc))
                    OnError(exc);
            }
        }

#if NETSTANDARD
        private async void AuthenticateAsClientAsync(SslStream sslStream, SecurityOption securityOption)
        {
            try
            {
                await sslStream.AuthenticateAsClientAsync(HostName, securityOption.Certificates, securityOption.EnabledSslProtocols, false);
            }
            catch(Exception e)
            {
                EnsureSocketClosed();
                OnError(e);
                return;
            }
            
            OnSslStreamConnected(sslStream);
        }
#endif
        
        private void OnSslStreamConnected(SslStream sslStream)
        {
            m_SslStream = sslStream;

            OnConnected();

            if(Buffer.Array == null)
            {
                var receiveBufferSize = ReceiveBufferSize;

                if (receiveBufferSize <= 0)
                    receiveBufferSize = DefaultReceiveBufferSize;

                ReceiveBufferSize = receiveBufferSize;

                Buffer = new ArraySegment<byte>(new byte[receiveBufferSize]);
            }

            BeginRead();
        }
        
#if !NETSTANDARD
        private void OnAuthenticated(IAsyncResult result)
        {
            var sslStream = result.AsyncState as SslStream;

            if(sslStream == null)
            {
                EnsureSocketClosed();
                OnError(new NullReferenceException("Ssl Stream is null OnAuthenticated"));
                return;
            }

            try
            {
                sslStream.EndAuthenticateAsClient(result);
            }
            catch(Exception e)
            {
                EnsureSocketClosed();
                OnError(e);
                return;
            }

            OnSslStreamConnected(sslStream);
        }

        private void OnDataRead(IAsyncResult result)
        {
            var state = result.AsyncState as SslAsyncState;

            if (state == null || state.SslStream == null)
            {
                OnError(new NullReferenceException("Null state or stream."));
                return;
            }

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
#endif

        void BeginRead()
        {
#if NETSTANDARD
            ReadAsync();
#else
            StartRead();
#endif
        }
        
#if NETSTANDARD
        private async void ReadAsync()
        {
            while (IsConnected)
            {
                var client = Client;

                if (client == null || m_SslStream == null)
                    return;
                
                var buffer = Buffer;
                
                var length = 0;
                
                try
                {
                    length = await m_SslStream.ReadAsync(buffer.Array, buffer.Offset, buffer.Count, CancellationToken.None);
                }
                catch (Exception e)
                {
                    if (!IsIgnorableException(e))
                        OnError(e);

                    if (EnsureSocketClosed(Client))
                        OnClosed();

                    return;
                }

                if (length == 0)
                {
                    if (EnsureSocketClosed(Client))
                        OnClosed();

                    return;
                }

                OnDataReceived(buffer.Array, buffer.Offset, length);
            }
        }
#else

    void StartRead()
    {
        var client = Client;

        if (client == null || m_SslStream == null)
            return;

        try
        {
            var buffer = Buffer;
            m_SslStream.BeginRead(buffer.Array, buffer.Offset, buffer.Count, OnDataRead, new SslAsyncState { SslStream = m_SslStream, Client = client });
        }
        catch (Exception e)
        {
            if (!IsIgnorableException(e))
                OnError(e);

            if (EnsureSocketClosed(client))
                OnClosed();
        }
    }

#endif

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
#if !NETSTANDARD
            var callback = ServicePointManager.ServerCertificateValidationCallback;

            if (callback != null)
                return callback(sender, certificate, chain, sslPolicyErrors);
#endif

            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            // only has certificate name mismatch error
            if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) == SslPolicyErrors.RemoteCertificateNameMismatch)
            {
                if (Security.AllowNameMismatchCertificate)
                    return true;
            }

            if (!Security.AllowUnstrustedCertificate)
            {
                OnError(new Exception(sslPolicyErrors.ToString()));
                return false;
            }

            // not only a remote certificate error
            if (sslPolicyErrors != SslPolicyErrors.None && sslPolicyErrors != SslPolicyErrors.RemoteCertificateChainErrors)
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
#if !NETSTANDARD
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

            if (state == null || state.SslStream == null)
            {
                OnError(new NullReferenceException("State of Ssl stream is null."));
                return;
            }

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
#else
        protected override void SendInternal(PosList<ArraySegment<byte>> items)
        {
            SendInternalAsync(items);
        }
        
        private async void SendInternalAsync(PosList<ArraySegment<byte>> items)
        {
            try
            {
                for (int i = items.Position; i < items.Count; i++)
                {
                    var item = items[items.Position];
                    await m_SslStream.WriteAsync(item.Array, item.Offset, item.Count, CancellationToken.None);
                }
                
                m_SslStream.Flush();
            }
            catch (Exception e)
            {
                if (!IsIgnorableException(e))
                    OnError(e);

                if (EnsureSocketClosed(Client))
                    OnClosed();
                    
                return;
            }
            
            OnSendingCompleted();
        }
        
#endif

        public override void Close()
        {
            var sslStream = m_SslStream;

            if (sslStream != null)
            {
#if !NETSTANDARD
                sslStream.Close();
#endif
                sslStream.Dispose();
                m_SslStream = null;
            }

            base.Close();
        }
    }
}
