using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SuperSocket.ClientEngine
{
    public static partial class ConnectAsyncExtension
    {
        public static void ConnectAsync(this EndPoint remoteEndPoint, Action<Socket, object, SocketAsyncEventArgs> connectedCallback, object state)
        {
            var e = CreateSocketAsyncEventArgs(remoteEndPoint, connectedCallback, state);
            Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, e);
        }
    }
}
