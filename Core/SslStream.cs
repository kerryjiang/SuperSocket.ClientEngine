using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Org.BouncyCastle.Crypto.Tls;

namespace System.Net.Security
{
    public class SslStream : Stream
    {
        class AsyncResult : IAsyncResult
        {
            public object AsyncState { get; set; }

            public WaitHandle AsyncWaitHandle { get; set; }

            public bool CompletedSynchronously { get; set; }

            public bool IsCompleted { get; set; }

            public AsyncCallback Callback { get; set; }

            public Exception Exception { get; set; }
        }

        private TlsProtocolHandler m_TlsHandler;

        private Stream m_InnerStream;
        private Stream m_SecureStream;

        public SslStream(Stream innerStream)
        {
            if (innerStream == null)
                throw new ArgumentNullException("innerStream");

            m_InnerStream = innerStream;
            m_SecureStream = innerStream;
        }

        public IAsyncResult BeginAuthenticateAsClient(string targetHost, AsyncCallback asyncCallback, Object asyncState)
        {
            var waitCallback = new WaitCallback(StartAuthentication);

            var result = new AsyncResult();
            result.Callback = asyncCallback;
            result.AsyncState = asyncState;

            ThreadPool.QueueUserWorkItem(waitCallback, result);
            return result;
        }

        void StartAuthentication(object state)
        {
            var result = state as AsyncResult;

            m_TlsHandler = new TlsProtocolHandler(m_InnerStream);

            try
            {
#pragma warning disable 0612,0618
                m_TlsHandler.Connect(new LegacyTlsClient(new AlwaysValidVerifyer()));
#pragma warning restore 0612,0618

                m_SecureStream = m_TlsHandler.Stream;
            }
            catch (Exception e)
            {
                result.Exception = e;
            }
            finally
            {
                result.IsCompleted = true;

                var callback = result.Callback;

                if (callback != null)
                    callback(result);
            }
        }

        public void EndAuthenticateAsClient(IAsyncResult asyncResult)
        {
            var result = asyncResult as AsyncResult;

            if(result.Exception != null)
                throw result.Exception;
        }

        public override bool CanRead
        {
            get { return m_SecureStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return m_SecureStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return m_SecureStream.CanWrite; }
        }

        public override void Flush()
        {
            m_SecureStream.Flush();
        }

        public override long Length
        {
            get { return m_SecureStream.Length; }
        }

        public override long Position
        {
            get
            {
                return m_SecureStream.Position;
            }
            set
            {
                m_SecureStream.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return m_SecureStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return m_SecureStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            m_SecureStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            m_SecureStream.Write(buffer, offset, count);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return m_SecureStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var waitCallback = new WaitCallback(StartSend);

            var result = new AsyncResult();
            result.AsyncState = state;
            result.Callback = callback;
            result.CompletedSynchronously = false;

            ThreadPool.QueueUserWorkItem(waitCallback, new object[] { buffer, offset, count, result });
            return result;
        }

        private void StartSend(object state)
        {
            var stateArr = state as object[];
            var result = stateArr[3] as AsyncResult;

            try
            {
                m_SecureStream.Write((byte[])stateArr[0], (int)stateArr[1], (int)stateArr[2]);
                m_SecureStream.Flush();
                result.IsCompleted = true;
            }
            catch (Exception e)
            {
                result.Exception = e;
            }
            finally
            {
                if (result.Callback != null)
                    result.Callback(result);
            }
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return m_SecureStream.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            var result = asyncResult as AsyncResult;

            if (result.Exception != null)
                throw result.Exception;
        }

        public override void Close()
        {
            m_SecureStream.Close();
        }

        public override int ReadByte()
        {
            return m_SecureStream.ReadByte();
        }

        public override void WriteByte(byte value)
        {
            m_SecureStream.WriteByte(value);
        }

        public override int ReadTimeout
        {
            get
            {
                return m_SecureStream.ReadTimeout;
            }
            set
            {
                m_SecureStream.ReadTimeout = value;
            }
        }

        public override int WriteTimeout
        {
            get
            {
                return m_SecureStream.WriteTimeout;
            }
            set
            {
                m_SecureStream.WriteTimeout = value;
            }
        }

        public override bool CanTimeout
        {
            get
            {
                return m_SecureStream.CanTimeout;
            }
        }
    }
}
