using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.ProtoBase;
using System.Net;

namespace SuperSocket.ClientEngine
{
    public abstract class EasyClientBase : IBufferState
    {
        private IClientSession m_Session;
        private TaskCompletionSource<bool> m_ConnectTaskSource;
        private TaskCompletionSource<bool> m_CloseTaskSource;
        private bool m_Connected = false;

        protected IPipelineProcessor PipeLineProcessor { get; set; }

        public int ReceiveBufferSize { get; set; }

        public EasyClientBase()
        {

        }

        public bool IsConnected { get { return m_Connected; } }

        public Task<bool> ConnectAsync(EndPoint serverEndPoint)
        {
            if (PipeLineProcessor == null)
                throw new Exception("This client has not been initialized.");

            m_ConnectTaskSource = new TaskCompletionSource<bool>();
            m_Session = new AsyncTcpSession(serverEndPoint, 4096);
            m_Session.Connected += new EventHandler(m_Session_Connected);
            m_Session.Error += new EventHandler<ErrorEventArgs>(m_Session_Error);
            m_Session.Closed +=new EventHandler(m_Session_Closed);
            m_Session.DataReceived += new EventHandler<DataEventArgs>(m_Session_DataReceived);

            if (ReceiveBufferSize > 0)
                m_Session.ReceiveBufferSize = ReceiveBufferSize;

            m_Session.Connect();
            return m_ConnectTaskSource.Task;
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
            if(m_Session != null && !m_Connected)
            {
                m_CloseTaskSource = new TaskCompletionSource<bool>();
                m_Session.Close();
                return m_CloseTaskSource.Task;
            }

            return null;
        }

        void m_Session_DataReceived(object sender, DataEventArgs e)
        {
            PipeLineProcessor.Process(new ArraySegment<byte>(e.Data, e.Offset, e.Length), this as IBufferState);
        }

        void m_Session_Error(object sender, ErrorEventArgs e)
        {
            if (!m_Connected)
            {
                m_ConnectTaskSource.SetResult(false);
                m_ConnectTaskSource = null;
            }

            OnError(e);
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
            m_ConnectTaskSource.SetResult(true);
            m_ConnectTaskSource = null;
            m_Connected = true;

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
