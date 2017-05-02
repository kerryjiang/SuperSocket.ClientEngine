using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace SuperSocket.ClientEngine
{
    public static partial class ConnectAsyncExtension
    {
        class DnsConnectState
        {
            public IPAddress[] Addresses { get; set; }

            public int NextAddressIndex { get; set; }

            public int Port { get; set; }

            public Socket Socket4 { get; set; }

            public Socket Socket6 { get; set; }

            public object State { get; set; }

            public ConnectedCallback Callback { get; set; }

            public EndPoint LocalEndPoint { get; set; }
        }

        private static void ConnectAsyncInternal(this EndPoint remoteEndPoint, EndPoint localEndPoint, ConnectedCallback callback, object state)
        {
            if (remoteEndPoint is DnsEndPoint)
            {
                var dnsEndPoint = (DnsEndPoint)remoteEndPoint;

                var asyncResult = Dns.BeginGetHostAddresses(dnsEndPoint.Host, OnGetHostAddresses,
                    new DnsConnectState
                    {
                        Port = dnsEndPoint.Port,
                        Callback = callback,
                        State = state,
                        LocalEndPoint = localEndPoint
                    });

                if (asyncResult.CompletedSynchronously)
                    OnGetHostAddresses(asyncResult);
            }
            else
            {
                var e = CreateSocketAsyncEventArgs(remoteEndPoint, callback, state);
                var socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                if (localEndPoint != null)
                {
                    socket.ExclusiveAddressUse = false;
                    socket.Bind(localEndPoint);
                }

                socket.ConnectAsync(e);
            }
        }

        private static IPAddress GetNextAddress(DnsConnectState state, out Socket attempSocket)
        {
            IPAddress address = null;
            attempSocket = null;

            var currentIndex = state.NextAddressIndex;

            while(attempSocket == null)
            {
                if (currentIndex >= state.Addresses.Length)
                    return null;

                address = state.Addresses[currentIndex++];

                if (address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    attempSocket = state.Socket6;
                }
                else if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    attempSocket = state.Socket4;
                }
            }

            state.NextAddressIndex = currentIndex;
            return address;
        }

        static partial void CreateAttempSocket(DnsConnectState connectState);

        private static void OnGetHostAddresses(IAsyncResult result)
        {
            var connectState = result.AsyncState as DnsConnectState;

            IPAddress[] addresses;

            try
            {
                addresses = Dns.EndGetHostAddresses(result);
            }
            catch (Exception e)
            {
                connectState.Callback(null, connectState.State, null, e);
                return;
            }

            if (addresses == null || addresses.Length <= 0)
            {
                connectState.Callback(null, connectState.State, null, new SocketException((int)SocketError.HostNotFound));
                return;
            }

            connectState.Addresses = addresses;

            CreateAttempSocket(connectState);

            Socket attempSocket;

            var address = GetNextAddress(connectState, out attempSocket);

            if (address == null)
            {
                connectState.Callback(null, connectState.State, null, new SocketException((int)SocketError.AddressFamilyNotSupported));
                return;
            }

            if (connectState.LocalEndPoint != null)
            {
                try
                {
                    attempSocket.ExclusiveAddressUse = false;
                    attempSocket.Bind(connectState.LocalEndPoint);
                }
                catch (Exception e)
                {
                    connectState.Callback(null, connectState.State, null, e);
                    return;
                }                
            }

            var socketEventArgs = new SocketAsyncEventArgs();
            socketEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(SocketConnectCompleted);
            var ipEndPoint = new IPEndPoint(address, connectState.Port);
            socketEventArgs.RemoteEndPoint = ipEndPoint;

            socketEventArgs.UserToken = connectState;

            if (!attempSocket.ConnectAsync(socketEventArgs))
                SocketConnectCompleted(attempSocket, socketEventArgs);
        }

        static void SocketConnectCompleted(object sender, SocketAsyncEventArgs e)
        {
            var connectState = e.UserToken as DnsConnectState;

            if (e.SocketError == SocketError.Success)
            {
                ClearSocketAsyncEventArgs(e);
                connectState.Callback((Socket)sender, connectState.State, e, null);
                return;
            }

            if (e.SocketError != SocketError.HostUnreachable && e.SocketError != SocketError.ConnectionRefused)
            {
                ClearSocketAsyncEventArgs(e);
                connectState.Callback(null, connectState.State, e, null);
                return;
            }

            Socket attempSocket;

            var address = GetNextAddress(connectState, out attempSocket);

            if (address == null)
            {
                ClearSocketAsyncEventArgs(e);
                e.SocketError = SocketError.HostUnreachable;
                connectState.Callback(null, connectState.State, e, null);
                return;
            }

            e.RemoteEndPoint = new IPEndPoint(address, connectState.Port);

            if (!attempSocket.ConnectAsync(e))
                SocketConnectCompleted(attempSocket, e);
        }

        private static void ClearSocketAsyncEventArgs(SocketAsyncEventArgs e)
        {
            e.Completed -= SocketConnectCompleted;
            e.UserToken = null;
        }
    }
}
