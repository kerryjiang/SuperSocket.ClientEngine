using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace SuperSocket.ClientEngine
{
    public static partial class ConnectAsyncExtension
    {
        public static void ConnectAsync(this EndPoint remoteEndPoint, ConnectedCallback callback, object state)
        {
            ConnectAsyncInternal(remoteEndPoint, callback, state);
        }

        static partial void CreateAttempSocket(DnsConnectState connectState)
        {
            if (Socket.OSSupportsIPv6)
                connectState.Socket6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

            if(Socket.OSSupportsIPv4)
                connectState.Socket4 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
    }
}