using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace SuperSocket.ClientEngine.Common
{
    public static partial class ConnectAsyncExtension
    {
        class DnsConnectState
        {
            public IPAddress[] Addresses { get; set; }

            public int CurrentConnectIndex { get; set; }

            public int Port { get; set; }

            public Socket Socket { get; set; }

            public object State { get; set; }

            public Action<Socket, object, SocketAsyncEventArgs> ConnectedCallback { get; set; }
        }

        public static void ConnectAsync(this EndPoint remoteEndPoint, Action<Socket, object, SocketAsyncEventArgs> connectedCallback, object state)
        {
            if (remoteEndPoint is DnsEndPoint)
            {
                var dnsEndPoint = (DnsEndPoint)remoteEndPoint;

                Dns.BeginGetHostAddresses(dnsEndPoint.Host, OnGetHostAddresses,
                    new DnsConnectState
                    {
                        Port = dnsEndPoint.Port,
                        ConnectedCallback = connectedCallback,
                        State = state
                    });
            }
            else
            {
                var e = CreateSocketAsyncEventArgs(remoteEndPoint, connectedCallback, state);
                var socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.ConnectAsync(e);
            }
        }

        private static void OnGetHostAddresses(IAsyncResult result)
        {
            IPAddress[] addresses = Dns.EndGetHostAddresses(result);

            var connectState = result.AsyncState as DnsConnectState;

            if (!Socket.OSSupportsIPv6)
                addresses = addresses.Where(a => a.AddressFamily == AddressFamily.InterNetwork).ToArray();
            else
            {
                //IPv4 address in higher priority
                addresses = addresses.OrderBy(a => a.AddressFamily == AddressFamily.InterNetwork ? 0 : 1).ToArray();
            }

            if (addresses.Length <= 0)
            {
                connectState.ConnectedCallback(null, connectState.State, null);
                return;
            }

            var socketEventArgs = new SocketAsyncEventArgs();
            socketEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(SocketConnectCompleted);

            connectState.Addresses = addresses;

            var ipEndPoint = new IPEndPoint(addresses[0], connectState.Port);
            socketEventArgs.RemoteEndPoint = ipEndPoint;

            var socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            connectState.Socket = socket;

            socketEventArgs.UserToken = connectState;

            if (!socket.ConnectAsync(socketEventArgs))
                SocketConnectCompleted(socket, socketEventArgs);
        }

        static void SocketConnectCompleted(object sender, SocketAsyncEventArgs e)
        {
            var connectState = e.UserToken as DnsConnectState;

            if (e.SocketError != SocketError.Success)
            {
                if (e.SocketError != SocketError.HostUnreachable && e.SocketError != SocketError.ConnectionRefused)
                {
                    ClearSocketAsyncEventArgs(e);
                    connectState.ConnectedCallback(null, connectState.State, e);
                    return;
                }

                if (connectState.Addresses.Length <= (connectState.CurrentConnectIndex + 1))
                {
                    ClearSocketAsyncEventArgs(e);
                    e.SocketError = SocketError.HostUnreachable;
                    connectState.ConnectedCallback(null, connectState.State, e);
                    return;
                }

                var currentConnectIndex = connectState.CurrentConnectIndex + 1;
                var currentIpAddress = connectState.Addresses[currentConnectIndex];

                e.RemoteEndPoint = new IPEndPoint(currentIpAddress, connectState.Port);
                connectState.CurrentConnectIndex = currentConnectIndex;

                var socket = new Socket(currentIpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                connectState.Socket = socket;

                if (!socket.ConnectAsync(e))
                    SocketConnectCompleted(socket, e);

                return;
            }

            ClearSocketAsyncEventArgs(e);
            connectState.ConnectedCallback(connectState.Socket, connectState.State, e);
        }

        private static void ClearSocketAsyncEventArgs(SocketAsyncEventArgs e)
        {
            e.Completed -= SocketConnectCompleted;
            e.UserToken = null;
        }
    }
}
