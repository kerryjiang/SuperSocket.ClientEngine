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

            if (localEndPoint != null)
            {
                var socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.ExclusiveAddressUse = false;
                socket.Bind(localEndPoint);
                socket.ConnectAsync(e);
            }
            else
            {
                Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, e);
            }            
#else
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            
            if (localEndPoint != null)
            {
                socket.ExclusiveAddressUse = false;
                socket.Bind(localEndPoint);
            }
                
            socket.ConnectAsync(e);
#endif
        }
    }
}
