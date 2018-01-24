using System.Net;
using System.Net.Sockets;

namespace SuperSocket.ClientEngine
{
    public static partial class ConnectAsyncExtension
    {
        public static void ConnectAsync(this EndPoint remoteEndPoint, EndPoint localEndPoint, ConnectedCallback callback, object state)
        {
            ConnectAsyncInternal(remoteEndPoint, localEndPoint, callback, state);
        }

        static partial void CreateAttempSocket(DnsConnectState connectState)
        {
            if (Socket.OSSupportsIPv6)
                connectState.Socket6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

            connectState.Socket4 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
    }
}