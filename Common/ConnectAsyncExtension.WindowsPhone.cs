using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SuperSocket.ClientEngine
{
    public static partial class ConnectAsyncExtension
    {
        public static void ConnectAsync(this EndPoint remoteEndPoint, ConnectedCallback callback, object state)
        {
            var e = CreateSocketAsyncEventArgs(remoteEndPoint, callback, state);
            Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, e);
        }
    }
}
