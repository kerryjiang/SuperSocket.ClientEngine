using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace SuperSocket.ClientEngine
{
    public class AsyncTcpSession : TcpClientSession
    {
        private SocketAsyncEventArgs m_SocketEventArgs;
        private SocketAsyncEventArgs m_SocketEventArgsSend;

        public AsyncTcpSession()
            : base()
        {

        }

        protected override void SocketEventArgsCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Connect)
            {
                ProcessConnect(sender as Socket, null, e, null);
                return;
            }

            ProcessReceive(e);
        }

        protected override void SetBuffer(ArraySegment<byte> bufferSegment)
        {
            base.SetBuffer(bufferSegment);

            if (m_SocketEventArgs != null)
            {
                m_SocketEventArgs.SetBuffer(bufferSegment.Array, bufferSegment.Offset, bufferSegment.Count);
            }
        }

        protected override void OnGetSocket(SocketAsyncEventArgs e)
        {
            if (Buffer.Array == null)
            {
                var receiveBufferSize = ReceiveBufferSize;

                if (receiveBufferSize <= 0)
                    receiveBufferSize = DefaultReceiveBufferSize;

                ReceiveBufferSize = receiveBufferSize;

                Buffer = new ArraySegment<byte>(new byte[receiveBufferSize]);
            }

            e.SetBuffer(Buffer.Array, Buffer.Offset, Buffer.Count);

            m_SocketEventArgs = e;

            OnConnected();
            StartReceive();
        }

        private void BeginReceive()
        {
            if (!Client.ReceiveAsync(m_SocketEventArgs))
                ProcessReceive(m_SocketEventArgs);
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                if(EnsureSocketClosed())
                    OnClosed();
                if(!IsIgnorableSocketError((int)e.SocketError))
                    OnError(new SocketException((int)e.SocketError));
                return;
            }

            if (e.BytesTransferred == 0)
            {
                if(EnsureSocketClosed())
                    OnClosed();
                return;
            }

            OnDataReceived(e.Buffer, e.Offset, e.BytesTransferred);
            StartReceive();
        }

        void StartReceive()
        {
            bool raiseEvent;

            var client = Client;

            if (client == null)
                return;

            try
            {
                raiseEvent = client.ReceiveAsync(m_SocketEventArgs);
            }
            catch (SocketException exc)
            {
                int errorCode;

#if !NETFX_CORE
                errorCode = exc.ErrorCode;
#else
                errorCode = (int)exc.SocketErrorCode;
#endif

                if (!IsIgnorableSocketError(errorCode))
                    OnError(exc);

                if (EnsureSocketClosed(client))
                    OnClosed();

                return;
            }
            catch(Exception e)
            {
                if(!IsIgnorableException(e))
                    OnError(e);

                if (EnsureSocketClosed(client))
                    OnClosed();

                return;
            }

            if (!raiseEvent)
                ProcessReceive(m_SocketEventArgs);
        }

        protected override void SendInternal(PosList<ArraySegment<byte>> items)
        {
            if (m_SocketEventArgsSend == null)
            {
                m_SocketEventArgsSend = new SocketAsyncEventArgs();
                m_SocketEventArgsSend.Completed += new EventHandler<SocketAsyncEventArgs>(Sending_Completed);
            }

            bool raiseEvent;

            try
            {
                if (items.Count > 1)
                {
                    if (m_SocketEventArgsSend.Buffer != null)
                        m_SocketEventArgsSend.SetBuffer(null, 0, 0);

                    m_SocketEventArgsSend.BufferList = items;
                }
                else
                {
                    var currentItem = items[0];

                    try
                    {
                        if (m_SocketEventArgsSend.BufferList != null)
                            m_SocketEventArgsSend.BufferList = null;
                    }
                    catch//a strange NullReference exception
                    {
                    }

                    m_SocketEventArgsSend.SetBuffer(currentItem.Array, currentItem.Offset, currentItem.Count);
                }
                

                raiseEvent = Client.SendAsync(m_SocketEventArgsSend);
            }
            catch (SocketException exc)
            {
                int errorCode;

#if !NETFX_CORE
                errorCode = exc.ErrorCode;
#else
                errorCode = (int)exc.SocketErrorCode;
#endif

                if (EnsureSocketClosed() && !IsIgnorableSocketError(errorCode))
                    OnError(exc);

                return;
            }
            catch (Exception e)
            {
                if (EnsureSocketClosed() && IsIgnorableException(e))
                    OnError(e);
                return;
            }

            if (!raiseEvent)
                Sending_Completed(Client, m_SocketEventArgsSend);
        }

        void Sending_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success || e.BytesTransferred == 0)
            {
                if(EnsureSocketClosed())
                    OnClosed();

                if (e.SocketError != SocketError.Success && !IsIgnorableSocketError((int)e.SocketError))
                    OnError(new SocketException((int)e.SocketError));

                return;
            }

            OnSendingCompleted();
        }

        protected override void OnClosed()
        {
            if (m_SocketEventArgsSend != null)
            {
                m_SocketEventArgsSend.Dispose();
                m_SocketEventArgsSend = null;
            }

            if (m_SocketEventArgs != null)
            {
                m_SocketEventArgs.Dispose();
                m_SocketEventArgs = null;
            }

            base.OnClosed();
        }
    }
}
