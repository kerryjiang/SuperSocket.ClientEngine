using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using SuperSocket.ClientEngine;

namespace SuperSocket.ClientEngine.Proxy
{
    public class Socks4aConnector : Socks4Connector
    {
        private static Random m_Random = new Random();

#if SILVERLIGHT && !WINDOWS_PHONE
        public Socks4aConnector(EndPoint proxyEndPoint, SocketClientAccessPolicyProtocol clientAccessPolicyProtocol, string userID)
            : base(proxyEndPoint, clientAccessPolicyProtocol, userID)
        {

        }
#else
        public Socks4aConnector(EndPoint proxyEndPoint, string userID)
            : base(proxyEndPoint, userID)
        {

        }
#endif

        public override void Connect(EndPoint remoteEndPoint)
        {
            DnsEndPoint targetEndPoint = remoteEndPoint as DnsEndPoint;

            if (targetEndPoint == null)
            {
                OnCompleted(new ProxyEventArgs(new Exception("The argument 'remoteEndPoint' must be a DnsEndPoint")));
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

        protected override byte[] GetSendingBuffer(EndPoint targetEndPoint, out int actualLength)
        {
            var targetDnsEndPoint = targetEndPoint as DnsEndPoint;

            //The buffer size should be larger than 8, because it is required for receiving
            var bufferLength = Math.Max(8, (string.IsNullOrEmpty(UserID) ? 0 : ASCIIEncoding.GetMaxByteCount(UserID.Length)) + 5 + 4 + ASCIIEncoding.GetMaxByteCount(targetDnsEndPoint.Host.Length) + 1);
            var handshake = new byte[bufferLength];

            handshake[0] = 0x04;
            handshake[1] = 0x01;

            handshake[2] = (byte)(targetDnsEndPoint.Port / 256);
            handshake[3] = (byte)(targetDnsEndPoint.Port % 256);

            handshake[4] = 0x00;
            handshake[5] = 0x00;
            handshake[6] = 0x00;
            handshake[7] = (byte)m_Random.Next(1, 255);

            actualLength = 8;

            if (!string.IsNullOrEmpty(UserID))
            {
                actualLength += ASCIIEncoding.GetBytes(UserID, 0, UserID.Length, handshake, actualLength);
            }

            handshake[actualLength++] = 0x00;

            actualLength += ASCIIEncoding.GetBytes(targetDnsEndPoint.Host, 0, targetDnsEndPoint.Host.Length, handshake, actualLength);
            handshake[actualLength++] = 0x00;

            return handshake;
        }

        protected override void HandleFaultStatus(byte status)
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
