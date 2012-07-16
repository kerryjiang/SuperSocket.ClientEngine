using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;

namespace SuperSocket.ClientEngine
{
    public abstract class TcpClientSession : ClientSession
    {
        protected string HostName { get; private set; }

        private bool m_InConnecting = false;

        public TcpClientSession(EndPoint remoteEndPoint)
            : this(remoteEndPoint, 1024)
        {

        }

        public TcpClientSession(EndPoint remoteEndPoint, int receiveBufferSize)
            : base(remoteEndPoint)
        {
            ReceiveBufferSize = receiveBufferSize;

            var dnsEndPoint = remoteEndPoint as DnsEndPoint;

            if (dnsEndPoint != null)
            {
                HostName = dnsEndPoint.Host;
                return;
            }

            var ipEndPoint = remoteEndPoint as IPEndPoint;

            if (ipEndPoint != null)
                HostName = ipEndPoint.Address.ToString();
        }

        public override int ReceiveBufferSize
        {
            get
            {
                return base.ReceiveBufferSize;
            }

            set
            {
                if (Buffer.Array != null)
                    throw new Exception("ReceiveBufferSize cannot be set after the socket has been connected!");

                base.ReceiveBufferSize = value;
            }
        }

        protected virtual bool IsIgnorableException(Exception e)
        {
            if (e is System.ObjectDisposedException)
                return true;

            if (e is NullReferenceException)
                return true;

            return false;
        }

        protected bool IsIgnorableSocketError(int errorCode)
        {
            //SocketError.Shutdown = 10058
            //SocketError.ConnectionAborted = 10053
            //SocketError.ConnectionReset = 10054
            if (errorCode == 10058 || errorCode == 10053 || errorCode == 10053)
                return true;

            return false;
        }

#if SILVERLIGHT && !WINDOWS_PHONE
        private SocketClientAccessPolicyProtocol m_ClientAccessPolicyProtocol = SocketClientAccessPolicyProtocol.Http;

        public SocketClientAccessPolicyProtocol ClientAccessPolicyProtocol
        {
            get { return m_ClientAccessPolicyProtocol; }
            set { m_ClientAccessPolicyProtocol = value; }
        }
#endif

        protected abstract void SocketEventArgsCompleted(object sender, SocketAsyncEventArgs e);

        public override void Connect()
        {
            if (m_InConnecting)
                throw new Exception("The socket is connecting, cannot connect again!");

            if (Client != null)
                throw new Exception("The socket is connected, you neednt' connect again!");

            //If there is a proxy set, connect the proxy server by proxy connector
            if (Proxy != null)
            {
                Proxy.Completed += new EventHandler<ProxyEventArgs>(Proxy_Completed);
                Proxy.Connect(RemoteEndPoint);
                m_InConnecting = true;
                return;
            }

            m_InConnecting = true;

//WindowsPhone doesn't have this property
#if SILVERLIGHT && !WINDOWS_PHONE
            RemoteEndPoint.ConnectAsync(ClientAccessPolicyProtocol, ProcessConnect, null);
#else
            RemoteEndPoint.ConnectAsync(ProcessConnect, null);
#endif
        }

        void Proxy_Completed(object sender, ProxyEventArgs e)
        {
            Proxy.Completed -= new EventHandler<ProxyEventArgs>(Proxy_Completed);

            if (e.Connected)
            {
                ProcessConnect(e.Socket, null, null);
                return;
            }

            OnError(new Exception("proxy error", e.Exception));
            m_InConnecting = false;
        }

        protected void ProcessConnect(Socket socket, object state, SocketAsyncEventArgs e)
        {
            if (e != null && e.SocketError != SocketError.Success)
            {
                m_InConnecting = false;
                OnError(new SocketException((int)e.SocketError));
                return;
            }

            if (socket == null)
            {
                m_InConnecting = false;
                OnError(new SocketException((int)SocketError.ConnectionAborted));
                return;
            }

            if (e == null)
                e = new SocketAsyncEventArgs();

            e.Completed += SocketEventArgsCompleted;

            Client = socket;

            m_InConnecting = false;

#if !SILVERLIGHT
            //Set keep alive
            Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
#endif
            OnGetSocket(e);
        }

        protected abstract void OnGetSocket(SocketAsyncEventArgs e);

        protected bool EnsureSocketClosed()
        {
            return EnsureSocketClosed(null);
        }

        protected bool EnsureSocketClosed(Socket prevClient)
        {
            var client = Client;

            if (client == null)
                return false;

            var fireOnClosedEvent = true;

            if (prevClient != null && prevClient != client)//originalClient is previous disconnected socket, so we needn't fire event for it
            {
                client = prevClient;
                fireOnClosedEvent = false;
            }
            else
            {
                Client = null;
                IsSending = false;
            }

            if (client.Connected)
            {
                try
                {
                    client.Shutdown(SocketShutdown.Both);
                }
                catch
                {

                }
                finally
                {
                    try
                    {
                        client.Close();
                    }
                    catch
                    {

                    }
                }
            }

            return fireOnClosedEvent;
        }

        private void DetectConnected()
        {
            if (Client != null)
                return;

            throw new Exception("The socket is not connected!", new SocketException((int)SocketError.NotConnected));
        }

        private ConcurrentQueue<ArraySegment<byte>> m_SendingQueue = new ConcurrentQueue<ArraySegment<byte>>();

        protected volatile bool IsSending = false;

        public override void Send(byte[] data, int offset, int length)
        {
            DetectConnected();

            m_SendingQueue.Enqueue(new ArraySegment<byte>(data, offset, length));

            if (!IsSending)
            {
                DequeueSend();
            }
        }

        public override void Send(IList<ArraySegment<byte>> segments)
        {
            DetectConnected();

            for (var i = 0; i < segments.Count; i++)
                m_SendingQueue.Enqueue(segments[i]);

            if (!IsSending)
            {
                DequeueSend();
            }
        }

        protected bool DequeueSend()
        {
            IsSending = true;
            ArraySegment<byte> segment;

            if (!m_SendingQueue.TryDequeue(out segment))
            {
                IsSending = false;
                return false;
            }

            SendInternal(segment);
            return true;
        }

        protected abstract void SendInternal(ArraySegment<byte> segment);

        public override void Close()
        {
            if (EnsureSocketClosed())
                OnClosed();
        }
    }
}
