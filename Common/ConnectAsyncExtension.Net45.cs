using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SuperSocket.ClientEngine
{
    public static partial class ConnectAsyncExtension
    {
        public static void ConnectAsync(this EndPoint remoteEndPoint, EndPoint localEndPoint, ConnectedCallback callback, object state)
        {
            var e = CreateSocketAsyncEventArgs(remoteEndPoint, callback, state);
            
#if NETSTANDARD
            Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, e);
#else
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.ConnectAsync(e);
#endif
        }
    }
}
