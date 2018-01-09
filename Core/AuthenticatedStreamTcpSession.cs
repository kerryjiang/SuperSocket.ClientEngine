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
    public abstract class AuthenticatedStreamTcpSession : TcpClientSession
    {
        class StreamAsyncState
        {
            public AuthenticatedStream Stream { get; set; }

            public Socket Client { get; set; }

            public PosList<ArraySegment<byte>> SendingItems { get; set; }
        }

        private AuthenticatedStream m_Stream;
                

        public AuthenticatedStreamTcpSession()
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

        protected abstract void StartAuthenticatedStream(Socket client);

        protected override void OnGetSocket(SocketAsyncEventArgs e)
        {
            try
            {
                StartAuthenticatedStream(Client);
            }
            catch (Exception exc)
            {
                if (!IsIgnorableException(exc))
                    OnError(exc);
            }
        }
        
        protected void OnAuthenticatedStreamConnected(AuthenticatedStream stream)
        {
            m_Stream = stream;

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

        private void OnDataRead(IAsyncResult result)
        {
            var state = result.AsyncState as StreamAsyncState;

            if (state == null || state.Stream == null)
            {
                OnError(new NullReferenceException("Null state or stream."));
                return;
            }

            var stream = state.Stream;

            int length = 0;

            try
            {
                length = stream.EndRead(result);
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

                if (client == null || m_Stream == null)
                    return;
                
                var buffer = Buffer;
                
                var length = 0;
                
                try
                {
                    length = await m_Stream.ReadAsync(buffer.Array, buffer.Offset, buffer.Count, CancellationToken.None);
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

        if (client == null || m_Stream == null)
            return;

        try
        {
            var buffer = Buffer;
            m_Stream.BeginRead(buffer.Array, buffer.Offset, buffer.Count, OnDataRead, new StreamAsyncState { Stream = m_Stream, Client = client });
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
                m_Stream.BeginWrite(item.Array, item.Offset, item.Count,
                    OnWriteComplete, new StreamAsyncState { Stream = m_Stream, Client = client, SendingItems = items });
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
            var state = result.AsyncState as StreamAsyncState;

            if (state == null || state.Stream == null)
            {
                OnError(new NullReferenceException("State of Ssl stream is null."));
                return;
            }

            var stream = state.Stream;

            try
            {
                stream.EndWrite(result);
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
                m_Stream.Flush();
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
                    var item = items[i];
                    await m_Stream.WriteAsync(item.Array, item.Offset, item.Count, CancellationToken.None);
                }
                
                m_Stream.Flush();
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
            var stream = m_Stream;

            if (stream != null)
            {
#if !NETSTANDARD
                stream.Close();
#endif
                stream.Dispose();
                m_Stream = null;
            }

            base.Close();
        }
    }
}
