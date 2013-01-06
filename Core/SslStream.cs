using System;
using System.Net;
using System.Net.Sockets;
using Org.BouncyCastle.Crypto.Tls;
using System.Threading;
using System.IO;

namespace System.Net.Security
{
    public class SslStream : Stream
    {
        class AuthenticateResult : IAsyncResult
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

        public SslStream(Stream innerStream)
        {
            if (innerStream == null)
                throw new ArgumentNullException("innerStream");

            m_InnerStream = innerStream;
        }

        public IAsyncResult BeginAuthenticateAsClient(string targetHost, AsyncCallback asyncCallback, Object asyncState)
        {
            var waitCallback = new WaitCallback(StartAuthentication);

            var result = new AuthenticateResult();
            result.Callback = asyncCallback;
            result.AsyncState = asyncState;

            ThreadPool.QueueUserWorkItem(waitCallback, result);
            return result;
        }

        void StartAuthentication(object state)
        {
            var result = state as AuthenticateResult;

            m_TlsHandler = new TlsProtocolHandler(this);

            try
            {
                m_TlsHandler.Connect(new LegacyTlsClient(new AlwaysValidVerifyer()));
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
            var result = asyncResult as AuthenticateResult;

            if(result.Exception != null)
                throw result.Exception;
        }

        public override bool CanRead
        {
            get { return m_InnerStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return m_InnerStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return m_InnerStream.CanSeek; }
        }

        public override void Flush()
        {
            m_InnerStream.Flush();
        }

        public override long Length
        {
            get { return m_InnerStream.Length; }
        }

        public override long Position
        {
            get
            {
                return m_InnerStream.Position;
            }
            set
            {
                m_InnerStream.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return m_InnerStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return m_InnerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            m_InnerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            m_InnerStream.Write(buffer, offset, count);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return m_InnerStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return m_InnerStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return m_InnerStream.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            m_InnerStream.EndWrite(asyncResult);
        }
    }
}
