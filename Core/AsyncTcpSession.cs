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

        public AsyncTcpSession(EndPoint remoteEndPoint)
            : base(remoteEndPoint)
        {

        }

        public AsyncTcpSession(EndPoint remoteEndPoint, int receiveBufferSize)
            : base(remoteEndPoint, receiveBufferSize)
        {

        }

        protected override void SocketEventArgsCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Connect)
            {
                ProcessConnect(sender as Socket, null, e);
                return;
            }

            ProcessReceive(e);
        }

        protected override void OnGetSocket(SocketAsyncEventArgs e)
        {
            if (Buffer.Array == null)
                Buffer = new ArraySegment<byte>(new byte[ReceiveBufferSize], 0, ReceiveBufferSize);

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
                if(!IsIgnorableSocketError(exc.ErrorCode))
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

        protected override void SendInternal(ArraySegment<byte> segment)
        {
            if (m_SocketEventArgsSend == null)
            {
                m_SocketEventArgsSend = new SocketAsyncEventArgs();
                m_SocketEventArgsSend.Completed += new EventHandler<SocketAsyncEventArgs>(Sending_Completed);
            }

            m_SocketEventArgsSend.SetBuffer(segment.Array, segment.Offset, segment.Count);

            bool raiseEvent;

            try
            {
                raiseEvent = Client.SendAsync(m_SocketEventArgsSend);
            }
            catch (SocketException exc)
            {
                if (EnsureSocketClosed() && !IsIgnorableSocketError(exc.ErrorCode))
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
            if (e.LastOperation != SocketAsyncOperation.Send)
            {
                IsSending = false;
                return;
            }

            if (e.SocketError != SocketError.Success || e.BytesTransferred == 0)
            {
                IsSending = false;
                if(EnsureSocketClosed())
                    OnClosed();

                if (e.SocketError != SocketError.Success && !IsIgnorableSocketError((int)e.SocketError))
                    OnError(new SocketException((int)e.SocketError));

                return;
            }

            DequeueSend();
        }
    }
}
