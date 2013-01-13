using System;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace System.Net
{
    public class NetworkStream : Stream
    {
        class StreamAsyncResult : IAsyncResult
        {
            public StreamAsyncResult(SocketAsyncEventArgs e, AsyncCallback callback)
                : this(e, callback, null)
            {

            }

            public StreamAsyncResult(SocketAsyncEventArgs e, AsyncCallback callback, System.Threading.WaitHandle waitHandle)
            {
                SocketAsyncEventArgs = e;
                e.UserToken = this;
                Callback = callback;
                AsyncWaitHandle = waitHandle;
            }

            public SocketAsyncEventArgs SocketAsyncEventArgs { get; private set; }

            public AsyncCallback Callback { get; private set; }

            public object AsyncState { get; set; }

            public System.Threading.WaitHandle AsyncWaitHandle { get; private set; }

            public bool CompletedSynchronously { get; set; }

            public bool IsCompleted { get; set; }
        }

        private Socket m_Socket;

        private SocketAsyncEventArgs m_SendEventArgs;

        private SocketAsyncEventArgs m_ReceiveEventArgs;

        public NetworkStream(Socket socket)
        {
            m_Socket = socket;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {
            //Do nothing
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var resetEvent = new ManualResetEvent(false);
            var result = BeginRead(buffer, offset, count, OnComplete, resetEvent);

            resetEvent.WaitOne();
            return EndRead(result);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var resetEvent = new ManualResetEvent(false);
            var result = BeginWrite(buffer, offset, count, OnComplete, resetEvent);

            resetEvent.WaitOne();
            EndWrite(result);
        }

        private void OnComplete(IAsyncResult result)
        {
            ManualResetEvent resetEvent = (ManualResetEvent)result.AsyncState;
            resetEvent.Set();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var e = m_ReceiveEventArgs;
                 
            if (e == null)
            {
                e = new SocketAsyncEventArgs();
                e.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveEventCompleted);
                m_ReceiveEventArgs = e;
            }

            e.SetBuffer(buffer, offset, count);

            StreamAsyncResult result = new StreamAsyncResult(e, callback);
            result.AsyncState = state;

            var async = m_Socket.ReceiveAsync(e);

            if (!async)
            {
                result.CompletedSynchronously = true;
                ProcessReceive(e);
            }

            return result;
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            var result = asyncResult as StreamAsyncResult;

            var e = result.SocketAsyncEventArgs;

            if (e.SocketError != SocketError.Success)
            {
                throw new SocketException((int)e.SocketError);
            }

            return e.BytesTransferred;
        }

        void OnReceiveEventCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessReceive(e);
        }

        void ProcessReceive(SocketAsyncEventArgs e)
        {
            var result = e.UserToken as StreamAsyncResult;
            e.UserToken = null;

            result.IsCompleted = true;

            var callback = result.Callback;

            if (callback != null)
                callback(result);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var e = m_SendEventArgs;

            if (e == null)
            {
                e = new SocketAsyncEventArgs();
                e.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendEventCompleted);
                m_SendEventArgs = e;
            }

            e.SetBuffer(buffer, offset, count);

            StreamAsyncResult result = new StreamAsyncResult(e, callback);
            result.AsyncState = state;

            var async = m_Socket.SendAsync(e);

            if (!async)
            {
                result.CompletedSynchronously = true;
                ProcessSend(e);
            }

            return result;
        }

        void OnSendEventCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessSend(e);
        }

        void ProcessSend(SocketAsyncEventArgs e)
        {
            var result = e.UserToken as StreamAsyncResult;
            result.IsCompleted = true;
            e.UserToken = null;

            var callback = result.Callback;

            if (callback != null)
                callback(result);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            var result = asyncResult as StreamAsyncResult;

            var e = result.SocketAsyncEventArgs;

            if (e.SocketError != SocketError.Success)
            {
                throw new SocketException((int)e.SocketError);
            }
        }
    }
}
