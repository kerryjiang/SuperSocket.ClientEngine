using System;
using System.Net;
using System.Net.Sockets;

namespace SuperSocket.ClientEngine
{
    public delegate void ConnectedCallback(Socket socket, object state, SocketAsyncEventArgs e, Exception exception);

    public static partial class ConnectAsyncExtension
    {
        private class ConnectToken
        {
            public object State { get; set; }

            public ConnectedCallback Callback { get; set; }
        }

        private static void SocketAsyncEventCompleted(object sender, SocketAsyncEventArgs e)
        {
            e.Completed -= SocketAsyncEventCompleted;
            var token = (ConnectToken)e.UserToken;
            e.UserToken = null;
            token.Callback(sender as Socket, token.State, e, null);
        }

        private static SocketAsyncEventArgs CreateSocketAsyncEventArgs(EndPoint remoteEndPoint, ConnectedCallback callback, object state)
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