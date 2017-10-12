using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace SuperSocket.ClientEngine
{
    public static partial class ConnectAsyncExtension
    {
        private static readonly MethodInfo m_ConnectMethod;

        private static bool m_OSSupportsIPv4;

        static ConnectAsyncExtension()
        {
            //.NET 4.0 has this method but Mono doesn't have
            m_ConnectMethod = typeof(Socket).GetMethod("ConnectAsync", BindingFlags.Public | BindingFlags.Static);

            //Socket.OSSupportsIPv4 doesn't exist in Mono
            var pro_OSSupportsIPv4 = typeof(Socket).GetProperty("OSSupportsIPv4", BindingFlags.Public | BindingFlags.Static);

            if (pro_OSSupportsIPv4 != null)
            {
                m_OSSupportsIPv4 = (bool)pro_OSSupportsIPv4.GetValue(null, new object[0]);
            }
            else
            {
                m_OSSupportsIPv4 = true;
            }
        }

        internal static bool PreferDnsInAdvance()
        {
            return Environment.GetEnvironmentVariable("PREFER_DNS_IN_ADVANCE") != null;
        }

        public static void ConnectAsync(this EndPoint remoteEndPoint, EndPoint localEndPoint, ConnectedCallback callback, object state)
        {
            //Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, e);
            //Don't use Socket.ConnectAsync directly because Mono hasn't implement this method
            if (m_ConnectMethod != null && localEndPoint == null && !PreferDnsInAdvance())
                m_ConnectMethod.Invoke(null, new object[] { SocketType.Stream, ProtocolType.Tcp, CreateSocketAsyncEventArgs(remoteEndPoint, callback, state) });
            else
            {
                ConnectAsyncInternal(remoteEndPoint, localEndPoint, callback, state);
            }
        }

        static partial void CreateAttempSocket(DnsConnectState connectState)
        {
            if (Socket.OSSupportsIPv6)
                connectState.Socket6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

            if (m_OSSupportsIPv4)
                connectState.Socket4 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
    }
}
