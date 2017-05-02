using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using SuperSocket.ClientEngine;

namespace SuperSocket.ClientEngine.Proxy
{
    public abstract class ProxyConnectorBase : IProxyConnector
    {
        public EndPoint ProxyEndPoint { get; private set; }

        protected static Encoding ASCIIEncoding = new ASCIIEncoding();

#if SILVERLIGHT && !WINDOWS_PHONE
        protected SocketClientAccessPolicyProtocol ClientAccessPolicyProtocol { get; private set; }

        public ProxyConnectorBase(EndPoint proxyEndPoint, SocketClientAccessPolicyProtocol clientAccessPolicyProtocol)
        {
            ProxyEndPoint = proxyEndPoint;
            ClientAccessPolicyProtocol = clientAccessPolicyProtocol;
        }

#else
        public ProxyConnectorBase(EndPoint proxyEndPoint)
        {
            ProxyEndPoint = proxyEndPoint;
        }
#endif

        public abstract void Connect(EndPoint remoteEndPoint);

        private EventHandler<ProxyEventArgs> m_Completed;

        public event EventHandler<ProxyEventArgs> Completed
        {
            add { m_Completed += value; }
            remove { m_Completed -= value; }
        }

        protected void OnCompleted(ProxyEventArgs args)
        {
            if (m_Completed == null)
                return;

            m_Completed(this, args);
        }

        protected void OnException(Exception exception)
        {
            OnCompleted(new ProxyEventArgs(exception));
        }

        protected void OnException(string exception)
        {
            OnCompleted(new ProxyEventArgs(new Exception(exception)));
        }

        protected bool ValidateAsyncResult(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                var socketException = new SocketException((int)e.SocketError);
                OnCompleted(new ProxyEventArgs(new Exception(socketException.Message, socketException)));
                return false;
            }

            return true;
        }

        protected void AsyncEventArgsCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Send)
                ProcessSend(e);
            else
                ProcessReceive(e);
        }

        protected void StartSend(Socket socket, SocketAsyncEventArgs e)
        {
            bool raiseEvent = false;

            try
            {
                raiseEvent = socket.SendAsync(e);
            }
            catch (Exception exc)
            {
                OnException(new Exception(exc.Message, exc));
                return;
            }

            if (!raiseEvent)
            {
                ProcessSend(e);
            }
        }

        protected virtual void StartReceive(Socket socket, SocketAsyncEventArgs e)
        {
            bool raiseEvent = false;

            try
            {
                raiseEvent = socket.ReceiveAsync(e);
            }
            catch (Exception exc)
            {
                OnException(new Exception(exc.Message, exc));
                return;
            }

            if (!raiseEvent)
            {
                ProcessReceive(e);
            }
        }

        protected abstract void ProcessConnect(Socket socket, object targetEndPoint, SocketAsyncEventArgs e, Exception exception);

        protected abstract void ProcessSend(SocketAsyncEventArgs e);

        protected abstract void ProcessReceive(SocketAsyncEventArgs e);
    }
}
