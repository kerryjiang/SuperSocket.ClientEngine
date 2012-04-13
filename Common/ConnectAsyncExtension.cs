using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Reflection;
using System.Net;

namespace SuperSocket.ClientEngine
{
    public delegate void ConnectedCallback(Socket socket, object state, SocketAsyncEventArgs e);

    public static partial class ConnectAsyncExtension
    {
        class ConnectToken
        {
            public object State { get; set; }

            public ConnectedCallback Callback { get; set; }
        }

        static void SocketAsyncEventCompleted(object sender, SocketAsyncEventArgs e)
        {
            e.Completed -= SocketAsyncEventCompleted;
            var token = (ConnectToken)e.UserToken;
            e.UserToken = null;
            token.Callback(sender as Socket, token.State, e);
        }

        static SocketAsyncEventArgs CreateSocketAsyncEventArgs(EndPoint remoteEndPoint, ConnectedCallback callback, object state)
        {
            var e = new SocketAsyncEventArgs();

            e.UserToken = new ConnectToken
            {
                State = state,
                Callback = callback
            };

            e.RemoteEndPoint = remoteEndPoint;
            e.Completed += new EventHandler<SocketAsyncEventArgs>(SocketAsyncEventCompleted);

            return e;
        }
    }
}
