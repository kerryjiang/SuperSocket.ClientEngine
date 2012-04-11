using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SuperSocket.ClientEngine.Common
{
    public static partial class ConnectAsyncExtension
    {
        public static void ConnectAsync(this EndPoint remoteEndPoint, SocketClientAccessPolicyProtocol clientAccessPolicyProtocol, Action<Socket, object, SocketAsyncEventArgs> connectedCallback, object state)
        {
            var e = CreateSocketAsyncEventArgs(remoteEndPoint, connectedCallback, state);
            e.SocketClientAccessPolicyProtocol = clientAccessPolicyProtocol;
            Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, e);
        }
    }
}
