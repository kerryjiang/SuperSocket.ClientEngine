using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace SuperSocket.ClientEngine
{
    public static partial class ConnectAsyncExtension
    {
        internal static bool PreferIPv4Stack()
        {
            return Environment.GetEnvironmentVariable("PREFER_IPv4_STACK") != null;
        }

        public static void ConnectAsync(this EndPoint remoteEndPoint, EndPoint localEndPoint, ConnectedCallback callback, object state)
        {
            var e = CreateSocketAsyncEventArgs(remoteEndPoint, callback, state);
            
#if NETSTANDARD

            if (localEndPoint != null)
            {
                var socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    socket.ExclusiveAddressUse = false;
                    socket.Bind(localEndPoint);
                }
                catch (Exception exc)
                {
                    callback(null, state, null, exc);
                    return;
                }

                socket.ConnectAsync(e);
            }
            else
            {
                Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, e);
            }            
#else
            var socket = PreferIPv4Stack()
                ? new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) 
                : new Socket(SocketType.Stream, ProtocolType.Tcp);
            
            if (localEndPoint != null)
            {
                try
                {
                    socket.ExclusiveAddressUse = false;
                    socket.Bind(localEndPoint);
                }
                catch (Exception exc)
                {
                    callback(null, state, null, exc);
                    return;
                }
            }
                
            socket.ConnectAsync(e);
#endif
        }
    }
}
