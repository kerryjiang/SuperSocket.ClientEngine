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
        class ConnectToken
        {
            public object State { get; set; }

            public Action<Socket, object, SocketAsyncEventArgs> ConnectedCallback { get; set; }
        }

        static void SocketAsyncEventCompleted(object sender, SocketAsyncEventArgs e)
        {
            e.Completed -= SocketAsyncEventCompleted;
            var token = (ConnectToken)e.UserToken;
            e.UserToken = null;
            token.ConnectedCallback(sender as Socket, token.State, e);
        }

        static SocketAsyncEventArgs CreateSocketAsyncEventArgs(EndPoint remoteEndPoint, Action<Socket, object, SocketAsyncEventArgs> connectedCallback, object state)
        {
            var e = new SocketAsyncEventArgs();

            e.UserToken = new ConnectToken
            {
                State = state,
                ConnectedCallback = connectedCallback
            };

            e.RemoteEndPoint = remoteEndPoint;
            e.Completed += new EventHandler<SocketAsyncEventArgs>(SocketAsyncEventCompleted);

            return e;
        }
    }
}
