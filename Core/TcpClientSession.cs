using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SuperSocket.ClientEngine.Common;

namespace SuperSocket.ClientEngine.Core
{
    public abstract class TcpClientSession : ClientSession
    {
        protected string HostName { get; private set; }

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

        protected bool IsIgnorableException(Exception e)
        {
            if (e is System.ObjectDisposedException)
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
//WindowsPhone doesn't have this property
#if SILVERLIGHT && !WINDOWS_PHONE
            RemoteEndPoint.ConnectAsync(ClientAccessPolicyProtocol, ProcessConnect, null);
#else
            RemoteEndPoint.ConnectAsync(ProcessConnect, null);
#endif
        }

        protected void ProcessConnect(Socket socket, object state, SocketAsyncEventArgs e)
        {
            if (e != null && e.SocketError != SocketError.Success)
            {
                OnError(new SocketException((int)e.SocketError));
                return;
            }

            if (socket == null)
            {
                OnError(new SocketException((int)SocketError.ConnectionAborted));
                return;
            }

            if (e == null)
                e = new SocketAsyncEventArgs();

            e.Completed += SocketEventArgsCompleted;

            Client = socket;

#if !SILVERLIGHT
            //Set keep alive
            Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
#endif

            StartReceive(e);
        }

        protected abstract void StartReceive(SocketAsyncEventArgs e);

        protected bool EnsureSocketClosed()
        {
            if (Client == null)
                return false;

            if (Client.Connected)
            {
                try
                {
                    Client.Shutdown(SocketShutdown.Both);
                    Client.Close();
                }
                catch
                {

                }
            }

            Client = null;
            return true;
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
