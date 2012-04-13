using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SuperSocket.ClientEngine
{
    public static partial class ConnectAsyncExtension
    {
        public static void ConnectAsync(this EndPoint remoteEndPoint, SocketClientAccessPolicyProtocol clientAccessPolicyProtocol, ConnectedCallback callback, object state)
        {
            var e = CreateSocketAsyncEventArgs(remoteEndPoint, callback, state);
            e.SocketClientAccessPolicyProtocol = clientAccessPolicyProtocol;
            Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, e);
        }
    }
}
