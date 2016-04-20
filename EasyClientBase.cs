using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.ProtoBase;
using System.Net;
using System.Threading;

namespace SuperSocket.ClientEngine
{
    public abstract class EasyClientBase : IBufferState
    {
        private IClientSession m_Session;
        private TaskCompletionSource<bool> m_ConnectTaskSource;
        private TaskCompletionSource<bool> m_CloseTaskSource;
        private bool m_Connected = false;

        protected IPipelineProcessor PipeLineProcessor { get; set; }

#if !NETFX_CORE
        public SecurityOption Security { get; set; }
#endif

#if !SILVERLIGHT

        public EndPoint LocalEndPoint { get; set; }
#endif

#if !__IOS__
        public bool NoDelay { get; set; }
#endif

        public int ReceiveBufferSize { get; set; }

        public EasyClientBase()
        {

        }

        public bool IsConnected { get { return m_Connected; } }



#if AWAIT
        public async Task<bool> ConnectAsync(EndPoint remoteEndPoint)
        {
            if (PipeLineProcessor == null)
                throw new Exception("This client has not been initialized.");

            m_ConnectTaskSource = InitConnect(remoteEndPoint);
            return await m_ConnectTaskSource.Task;
        }
#else
        public Task<bool> ConnectAsync(EndPoint remoteEndPoint)
        {
            if (PipeLineProcessor == null)
                throw new Exception("This client has not been initialized.");

            m_ConnectTaskSource = InitConnect(remoteEndPoint);
            return m_ConnectTaskSource.Task;
        }
#endif

        private TcpClientSession GetUnderlyingSession()
        {
#if NETFX_CORE
            return new AsyncTcpSession();
#else
            var security = Security;

            if (security == null)
            {
                return new AsyncTcpSession();
            }

    #if SILVERLIGHT
            // no SSL/TLS enabled
            if (!security.EnabledSslProtocols)
            {
                return new AsyncTcpSession();
            }

            return new SslStreamTcpSession();
    #else
            // no SSL/TLS enabled
            if (security.EnabledSslProtocols == System.Security.Authentication.SslProtocols.None)
            {
                return new AsyncTcpSession();
            }

            return new SslStreamTcpSession()
            {
                Security = security
            };
    #endif
#endif
        }

        private TaskCompletionSource<bool> InitConnect(EndPoint remoteEndPoint)
        {
            var session = GetUnderlyingSession();

#if !SILVERLIGHT
            var localEndPoint = LocalEndPoint;

            if (localEndPoint != null)
            {
                session.LocalEndPoint = localEndPoint;
            }
#endif

#if !__IOS__
            session.NoDelay = NoDelay;
#endif

            session.Connected += new EventHandler(m_Session_Connected);
            session.Error += new EventHandler<ErrorEventArgs>(m_Session_Error);
            session.Closed += new EventHandler(m_Session_Closed);
            session.DataReceived += new EventHandler<DataEventArgs>(m_Session_DataReceived);

            if (ReceiveBufferSize > 0)
                session.ReceiveBufferSize = ReceiveBufferSize;

            m_Session = session;

            session.Connect(remoteEndPoint);
            
            return new TaskCompletionSource<bool>();
        }

        public void Send(ArraySegment<byte> segment)
        {
            if(!m_Connected || m_Session == null)
                throw new Exception("The socket is not connected.");

            m_Session.Send(segment);
        }

        public void Send(List<ArraySegment<byte>> segments)
        {
            if(!m_Connected || m_Session == null)
                throw new Exception("The socket is not connected.");

            m_Session.Send(segments);
        }

        public Task<bool> Close()
        {
            var session = m_Session;
            
            if(session != null && m_Connected)
            {
                var closeTaskSrc = new TaskCompletionSource<bool>();
                session.Close();
                m_CloseTaskSource = closeTaskSrc;
                return closeTaskSrc.Task;
            }

            return null;
        }

        void m_Session_DataReceived(object sender, DataEventArgs e)
        {
            var result = PipeLineProcessor.Process(new ArraySegment<byte>(e.Data, e.Offset, e.Length), this as IBufferState);

            // allocate new receive buffer if the previous one was cached
            if (result.State == ProcessState.Cached)
            {
                var session = m_Session;

                if (session != null)
                {
                    var bufferSetter = session as IBufferSetter;

                    if (bufferSetter != null)
                    {
                        bufferSetter.SetBuffer(new ArraySegment<byte>(new byte[session.ReceiveBufferSize]));
                    }
                }
            }
        }

        void m_Session_Error(object sender, ErrorEventArgs e)
        {
            if (!m_Connected)
            {
                FinishConnectTask(false);
            }

            OnError(e);
        }

        bool FinishConnectTask(bool result)
        {
            var connectTaskSource = m_ConnectTaskSource;

            if (connectTaskSource == null)
                return false;

            if (Interlocked.CompareExchange(ref m_ConnectTaskSource, null, connectTaskSource) == connectTaskSource)
            {
                connectTaskSource.SetResult(result);
                return true;
            }

            return false;
        }

        private void OnError(Exception e)
        {
            OnError(new ErrorEventArgs(e));
        }

        private void OnError(ErrorEventArgs args)
        {
            var handler = Error;

            if(handler != null)
                handler(this, args);
        }

        public event EventHandler<ErrorEventArgs> Error;

        void m_Session_Closed(object sender, EventArgs e)
        {
            m_Connected = false;

            var handler = Closed;

            if (handler != null)
                handler(this, EventArgs.Empty);

            if(m_CloseTaskSource != null)
            {
                m_CloseTaskSource.SetResult(true);
                m_CloseTaskSource = null;
            }
        }

        public event EventHandler Closed;

        void m_Session_Connected(object sender, EventArgs e)
        {
            m_Connected = true;
            FinishConnectTask(true);

            var handler = Connected;
            if(handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public event EventHandler Connected;

        int IBufferState.DecreaseReference()
        {
            return 0;
        }

        void IBufferState.IncreaseReference()
        {

        }
    }
}
