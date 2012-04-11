using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Reflection;
using System.Net;

namespace SuperSocket.ClientEngine.Common
{
    public static partial class ConnectAsyncExtension
    {
        private static readonly MethodInfo m_ConnectMethod;
        private static readonly bool m_IsMono;

        static ConnectAsyncExtension()
        {
            m_ConnectMethod = typeof(Socket).GetMethod("ConnectAsync", BindingFlags.Public | BindingFlags.Static);

            Type monoType = Type.GetType("Mono.Runtime");
            m_IsMono = monoType != null;
        }

        public static void ConnectAsync(this EndPoint remoteEndPoint, Action<Socket, object, SocketAsyncEventArgs> connectedCallback, object state)
        {
            var e = CreateSocketAsyncEventArgs(remoteEndPoint, connectedCallback, state);

            if (m_IsMono)
                m_ConnectMethod.Invoke(null, new object[] { e });
            else
                m_ConnectMethod.Invoke(null, new object[] { SocketType.Stream, ProtocolType.Tcp, e });
        }
    }
}
