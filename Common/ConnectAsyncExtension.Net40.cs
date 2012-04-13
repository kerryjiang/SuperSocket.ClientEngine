using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Reflection;
using System.Net;

namespace SuperSocket.ClientEngine
{
    public static partial class ConnectAsyncExtension
    {
        private static readonly MethodInfo m_ConnectMethod;

        static ConnectAsyncExtension()
        {
            //.NET 4.0 has this method but Mono doesn't have
            m_ConnectMethod = typeof(Socket).GetMethod("ConnectAsync", BindingFlags.Public | BindingFlags.Static);
        }

        public static void ConnectAsync(this EndPoint remoteEndPoint, ConnectedCallback callback, object state)
        {
            //Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, e);
            //Don't use Socket.ConnectAsync directly because Mono hasn't implement this method
            if (m_ConnectMethod != null)
                m_ConnectMethod.Invoke(null, new object[] { SocketType.Stream, ProtocolType.Tcp, CreateSocketAsyncEventArgs(remoteEndPoint, callback, state) });
            else
            {
                ConnectAsyncInternal(remoteEndPoint, callback, state);
            }
        }

        static partial void CreateAttempSocket(DnsConnectState connectState)
        {
            if (Socket.OSSupportsIPv6)
                connectState.Socket6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

            if (Socket.OSSupportsIPv4)
                connectState.Socket4 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
    }
}
