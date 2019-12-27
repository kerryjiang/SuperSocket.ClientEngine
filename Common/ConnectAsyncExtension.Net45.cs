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

            AddressFamily addressFamily = remoteEndPoint.AddressFamily;

            if (localEndPoint != null)
            {
                addressFamily = localEndPoint.AddressFamily;
            }

            var socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);

#else
            var socket = PreferIPv4Stack()
                ? new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                : new Socket(SocketType.Stream, ProtocolType.Tcp);
#endif

            try
            {
                if (localEndPoint != null)
                {
                    socket.ExclusiveAddressUse = false;
                    socket.Bind(localEndPoint);
                }

                bool wasAsync = socket.ConnectAsync(e);

                if (!wasAsync)
                {
                    callback(socket, state, e, null);
                }
            }
            catch (Exception exc)
            {
                callback(null, state, null, exc);
            }

        }
    }
}
