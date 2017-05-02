using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SuperSocket.ClientEngine;

namespace SuperSocket.ClientEngine.Proxy
{
    /// <summary>
    /// http://tools.ietf.org/html/rfc1928
    /// </summary>
    public class Socks5Connector : ProxyConnectorBase
    {
        enum SocksState
        {
            NotAuthenticated,
            Authenticating,
            Authenticated,
            FoundLength,
            Connected
        }

        class SocksContext
        {
            public Socket Socket { get; set; }

            public SocksState State { get; set; }

            public EndPoint TargetEndPoint { get; set; }

            public List<byte> ReceivedData { get; set; }

            public int ExpectedLength { get; set; }
        }

        private ArraySegment<byte> m_UserNameAuthenRequest;

        private static byte[] m_AuthenHandshake = new byte[] { 0x05, 0x02, 0x00, 0x02 };

#if SILVERLIGHT && !WINDOWS_PHONE
        public Socks5Connector(EndPoint proxyEndPoint, SocketClientAccessPolicyProtocol clientAccessPolicyProtocol)
            : base(proxyEndPoint, clientAccessPolicyProtocol)
        {

        }
#else
        public Socks5Connector(EndPoint proxyEndPoint)
            : base(proxyEndPoint)
        {

        }
#endif

#if SILVERLIGHT && !WINDOWS_PHONE
        public Socks5Connector(EndPoint proxyEndPoint, SocketClientAccessPolicyProtocol clientAccessPolicyProtocol, string username, string password)
            : base(proxyEndPoint, clientAccessPolicyProtocol)
#else
        public Socks5Connector(EndPoint proxyEndPoint, string username, string password)
            : base(proxyEndPoint)
#endif
        {
            if (string.IsNullOrEmpty(username))
                throw new ArgumentNullException("username");

            var buffer = new byte[3 + ASCIIEncoding.GetMaxByteCount(username.Length) + (string.IsNullOrEmpty(password) ? 0 : ASCIIEncoding.GetMaxByteCount(password.Length))];
            var actualLength = 0;

            buffer[0] = 0x05;
            var len = ASCIIEncoding.GetBytes(username, 0, username.Length, buffer, 2);

            if (len > 255)
                throw new ArgumentException("the length of username cannot exceed 255", "username");

            buffer[1] = (byte)len;

            actualLength = len + 2;

            if (!string.IsNullOrEmpty(password))
            {
                len = ASCIIEncoding.GetBytes(password, 0, password.Length, buffer, actualLength + 1);

                if (len > 255)
                    throw new ArgumentException("the length of password cannot exceed 255", "password");

                buffer[actualLength] = (byte)len;
                actualLength += len + 1;
            }
            else
            {
                buffer[actualLength] = 0x00;
                actualLength++;
            }

            m_UserNameAuthenRequest = new ArraySegment<byte>(buffer, 0, actualLength);
        }

        public override void Connect(EndPoint remoteEndPoint)
        {
            if (remoteEndPoint == null)
                throw new ArgumentNullException("remoteEndPoint");

            if (!(remoteEndPoint is IPEndPoint || remoteEndPoint is DnsEndPoint))
                throw new ArgumentException("remoteEndPoint must be IPEndPoint or DnsEndPoint", "remoteEndPoint");


            try
            {
#if SILVERLIGHT && !WINDOWS_PHONE
                ProxyEndPoint.ConnectAsync(ClientAccessPolicyProtocol, ProcessConnect, remoteEndPoint);
#elif WINDOWS_PHONE
                ProxyEndPoint.ConnectAsync(ProcessConnect, remoteEndPoint);
#else
                ProxyEndPoint.ConnectAsync(null, ProcessConnect, remoteEndPoint);
#endif
            }
            catch (Exception e)
            {
                OnException(new Exception("Failed to connect proxy server", e));
            }
        }

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

            e.UserToken = new SocksContext { TargetEndPoint = (EndPoint)targetEndPoint, Socket = socket, State = SocksState.NotAuthenticated };
            e.Completed += new EventHandler<SocketAsyncEventArgs>(AsyncEventArgsCompleted);

            e.SetBuffer(m_AuthenHandshake, 0, m_AuthenHandshake.Length);

            StartSend(socket, e);
        }

        protected override void ProcessSend(SocketAsyncEventArgs e)
        {
            if (!ValidateAsyncResult(e))
                return;

            var context = e.UserToken as SocksContext;

            if (context.State == SocksState.NotAuthenticated)
            {
                e.SetBuffer(0, 2);
                StartReceive(context.Socket, e);
            }
            else if (context.State == SocksState.Authenticating)
            {
                e.SetBuffer(0, 2);
                StartReceive(context.Socket, e);
            }
            else
            {
                e.SetBuffer(0, e.Buffer.Length);
                StartReceive(context.Socket, e);
            }
        }

        private bool ProcessAuthenticationResponse(Socket socket, SocketAsyncEventArgs e)
        {
            int total = e.BytesTransferred + e.Offset;

            if (total < 2)
            {
                e.SetBuffer(total, 2 - total);
                StartReceive(socket, e);
                return false;
            }
            else if (total > 2)
            {
                OnException("received length exceeded");
                return false;
            }

            if (e.Buffer[0] != 0x05)
            {
                OnException("invalid protocol version");
                return false;
            }

            return true;
        }

        protected override void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (!ValidateAsyncResult(e))
                return;

            var context = (SocksContext)e.UserToken;

            if (context.State == SocksState.NotAuthenticated)
            {
                if (!ProcessAuthenticationResponse(context.Socket, e))
                    return;

                var method = e.Buffer[1];

                if (method == 0x00)
                {
                    context.State = SocksState.Authenticated;
                    SendHandshake(e);
                    return;
                }
                else if (method == 0x02)
                {
                    context.State = SocksState.Authenticating;
                    AutheticateWithUserNamePassword(e);
                    return;
                }
                else if (method == 0xff)
                {
                    OnException("no acceptable methods were offered");
                    return;
                }
                else
                {
                    OnException("protocol error");
                    return;
                }
            }
            else if (context.State == SocksState.Authenticating)
            {
                if (!ProcessAuthenticationResponse(context.Socket, e))
                    return;

                var method = e.Buffer[1];

                if (method == 0x00)
                {
                    context.State = SocksState.Authenticated;
                    SendHandshake(e);
                    return;
                }
                else
                {
                    OnException("authentication failure");
                    return;
                }
            }
            else
            {
                byte[] data = new byte[e.BytesTransferred];
                Buffer.BlockCopy(e.Buffer, e.Offset, data, 0, e.BytesTransferred);

                context.ReceivedData.AddRange(data);

                if (context.ExpectedLength > context.ReceivedData.Count)
                {
                    StartReceive(context.Socket,e);
                    return;
                }
                else
                {
                    if (context.State != SocksState.FoundLength)
                    {
                        var addressType = context.ReceivedData[3];
                        int expectedLength;

                        if (addressType == 0x01)
                        {
                            expectedLength = 10;
                        }
                        else if (addressType == 0x03)
                        {
                            expectedLength = 4 + 1 + 2 + (int)context.ReceivedData[4];
                        }
                        else
                        {
                            expectedLength = 22;
                        }

                        if (context.ReceivedData.Count < expectedLength)
                        {
                            context.ExpectedLength = expectedLength;
                            StartReceive(context.Socket, e);
                            return;
                        }
                        else if (context.ReceivedData.Count > expectedLength)
                        {
                            OnException("response length exceeded");
                            return;
                        }
                        else
                        {
                            OnGetFullResponse(context);
                            return;
                        }
                    }
                    else
                    {
                        if (context.ReceivedData.Count > context.ExpectedLength)
                        {
                            OnException("response length exceeded");
                            return;
                        }

                        OnGetFullResponse(context);
                        return;
                    }
                }
            }
        }

        private void OnGetFullResponse(SocksContext context)
        {
            var response = context.ReceivedData;

            if (response[0] != 0x05)
            {
                OnException("invalid protocol version");
                return;
            }

            var status = response[1];

            if (status == 0x00)
            {
                OnCompleted(new ProxyEventArgs(context.Socket));
                return;
            }

            //0x01 = general failure
            //0x02 = connection not allowed by ruleset
            //0x03 = network unreachable
            //0x04 = host unreachable
            //0x05 = connection refused by destination host
            //0x06 = TTL expired
            //0x07 = command not supported / protocol error
            //0x08 = address type not supported

            string message = string.Empty;

            switch (status)
            {
                case (0x02):
                    message = "connection not allowed by ruleset";
                    break;

                case (0x03):
                    message = "network unreachable";
                    break;

                case (0x04):
                    message = "host unreachable";
                    break;

                case (0x05):
                    message = "connection refused by destination host";
                    break;

                case (0x06):
                    message = "TTL expired";
                    break;

                case (0x07):
                    message = "command not supported / protocol error";
                    break;

                case (0x08):
                    message = "address type not supported";
                    break;

                default:
                    message = "general failure";
                    break;
            }

            OnException(message);
        }

        private void SendHandshake(SocketAsyncEventArgs e)
        {
            var context = e.UserToken as SocksContext;

            var targetEndPoint = context.TargetEndPoint;

            byte[] buffer;
            int actualLength;
            int port = 0;

            if (targetEndPoint is IPEndPoint)
            {
                var endPoint = targetEndPoint as IPEndPoint;
                port = endPoint.Port;

                if (endPoint.AddressFamily == AddressFamily.InterNetwork)
                {
                    buffer = new byte[10];
                    buffer[3] = 0x01;

                    Buffer.BlockCopy(endPoint.Address.GetAddressBytes(), 0, buffer, 4, 4);
                }
                else if (endPoint.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    buffer = new byte[22];
                    buffer[3] = 0x04;

                    Buffer.BlockCopy(endPoint.Address.GetAddressBytes(), 0, buffer, 4, 16);
                }
                else
                {
                    OnException("unknown address family");
                    return;
                }

                actualLength = buffer.Length;
            }
            else
            {
                var endPoint = targetEndPoint as DnsEndPoint;

                port = endPoint.Port;

                var maxLen = 7 + ASCIIEncoding.GetMaxByteCount(endPoint.Host.Length);
                buffer = new byte[maxLen];

                buffer[3] = 0x03;

                actualLength = 5;
                actualLength += ASCIIEncoding.GetBytes(endPoint.Host, 0, endPoint.Host.Length, buffer, actualLength);
                actualLength += 2;
            }

            buffer[0] = 0x05;
            buffer[1] = 0x01;
            buffer[2] = 0x00;

            buffer[actualLength - 2] = (byte)(port / 256);
            buffer[actualLength - 1] = (byte)(port % 256);

            e.SetBuffer(buffer, 0, actualLength);

            context.ReceivedData = new List<byte>(actualLength + 5);
            context.ExpectedLength = 5; //When the client receive 5 bytes, we can know how many bytes should be received exactly

            StartSend(context.Socket, e);
        }

        private void AutheticateWithUserNamePassword(SocketAsyncEventArgs e)
        {
            var context = (SocksContext)e.UserToken;

            var socket = context.Socket;

            e.SetBuffer(m_UserNameAuthenRequest.Array, m_UserNameAuthenRequest.Offset, m_UserNameAuthenRequest.Count);

            StartSend(socket, e);
        }
    }
}
