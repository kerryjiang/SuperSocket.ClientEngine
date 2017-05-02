using System; 
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using SuperSocket.ClientEngine;

namespace SuperSocket.ClientEngine.Proxy
{
    class ReceiveState
    {
        public ReceiveState(byte[] buffer)
        {
            Buffer = buffer;
        }

        public byte[] Buffer { get; private set; }

        public int Length { get; set; }
    }

    public class Socks4Connector : ProxyConnectorBase
    {
        public string UserID { get; private set; }

#if SILVERLIGHT && !WINDOWS_PHONE
        public Socks4Connector(EndPoint proxyEndPoint, SocketClientAccessPolicyProtocol clientAccessPolicyProtocol, string userID)
            : base(proxyEndPoint, clientAccessPolicyProtocol)
        {
            UserID = userID;
        }
#else
        public Socks4Connector(EndPoint proxyEndPoint, string userID)
            : base(proxyEndPoint)
        {
            UserID = userID;
        }
#endif

        public override void Connect(EndPoint remoteEndPoint)
        {
            IPEndPoint targetEndPoint = remoteEndPoint as IPEndPoint;

            if (targetEndPoint == null)
            {
                OnCompleted(new ProxyEventArgs(new Exception("The argument 'remoteEndPoint' must be a IPEndPoint")));
                return;
            }

            try
            {
#if SILVERLIGHT && !WINDOWS_PHONE
                ProxyEndPoint.ConnectAsync(ClientAccessPolicyProtocol, ProcessConnect, targetEndPoint);
#elif WINDOWS_PHONE
                ProxyEndPoint.ConnectAsync(ProcessConnect, remoteEndPoint);
#else
                ProxyEndPoint.ConnectAsync(null, ProcessConnect, targetEndPoint);
#endif
            }
            catch (Exception e)
            {
                OnException(new Exception("Failed to connect proxy server", e));
            }
        }

        protected virtual byte[] GetSendingBuffer(EndPoint targetEndPoint, out int actualLength)
        {
            var targetIPEndPoint = targetEndPoint as IPEndPoint;
            var addressBytes = targetIPEndPoint.Address.GetAddressBytes();

            //The buffer size should be larger than 8, because it is required for receiving
            var bufferLength = Math.Max(8, (string.IsNullOrEmpty(UserID) ? 0 : ASCIIEncoding.GetMaxByteCount(UserID.Length)) + 5 + addressBytes.Length);
            var handshake = new byte[bufferLength];

            handshake[0] = 0x04;
            handshake[1] = 0x01;

            handshake[2] = (byte)(targetIPEndPoint.Port / 256);
            handshake[3] = (byte)(targetIPEndPoint.Port % 256);

            Buffer.BlockCopy(addressBytes, 0, handshake, 4, addressBytes.Length);

            actualLength = 4 + addressBytes.Length;

            if (!string.IsNullOrEmpty(UserID))
            {
                actualLength += ASCIIEncoding.GetBytes(UserID, 0, UserID.Length, handshake, actualLength);
            }

            handshake[actualLength++] = 0x00;

            return handshake;
        }

        /// <summary>
        /// Processes the connect.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="targetEndPoint">The target end point.</param>
        /// <param name="e">The <see cref="System.Net.Sockets.SocketAsyncEventArgs"/> instance containing the event data.</param>
        protected override void ProcessConnect(Socket socket, object targetEndPoint, SocketAsyncEventArgs e, Exception exception)
        {
            if (exception != null)
            {
                OnException(exception);
                return;
            }

            if (e != null)
            {
                if (!ValidateAsyncResult(e))
                    return;
            }

            if (socket == null)
            {
                OnException(new SocketException((int)SocketError.ConnectionAborted));
                return;
            }

            if (e == null)
                e = new SocketAsyncEventArgs();

            int actualLength;
            var sendingBuffer = GetSendingBuffer((EndPoint)targetEndPoint, out actualLength);

            e.SetBuffer(sendingBuffer, 0, actualLength);
            e.UserToken = socket;
            e.Completed += AsyncEventArgsCompleted;

            StartSend(socket, e);
        }

        protected override void ProcessSend(SocketAsyncEventArgs e)
        {
            if (!ValidateAsyncResult(e))
                return;

            e.SetBuffer(0, 8);
            StartReceive((Socket)e.UserToken, e);
        }

        private const int m_ValidResponseSize = 8;

        protected override void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (!ValidateAsyncResult(e))
                return;

            int total = e.Offset + e.BytesTransferred;

            if (total < m_ValidResponseSize)
            {
                e.SetBuffer(total, m_ValidResponseSize - total);
                StartReceive((Socket)e.UserToken, e);
                return;
            }
            else if (total == m_ValidResponseSize)
            {
                byte status = e.Buffer[1];

                //Succeced
                if (status == 0x5a)
                {
                    OnCompleted(new ProxyEventArgs((Socket)e.UserToken));
                    return;
                }

                HandleFaultStatus(status);
            }
            else// total > 8
            {
                OnException("socks protocol error: size of response cannot be larger than 8");
            }
        }

        protected virtual void HandleFaultStatus(byte status)
        {
            string message = string.Empty;

            switch (status)
            {
                case (0x5b):
                    message = "request rejected or failed";
                    break;
                case (0x5c):
                    message = "request failed because client is not running identd (or not reachable from the server)";
                    break;
                case (0x5d):
                    message = "request failed because client's identd could not confirm the user ID string in the reques";
                    break;
                default:
                    message = "request rejected for unknown error";
                    break;
            }

            OnException(message);
        }
    }
}
