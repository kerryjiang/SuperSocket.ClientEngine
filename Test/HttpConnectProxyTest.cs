using System.Threading.Tasks;
using Xunit;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using SuperSocket.ProtoBase;
using System.Text;
using SuperSocket.ClientEngine.Proxy;
using System.Threading;

namespace SuperSocket.ClientEngine.Test
{
    public class HttpConnectProxyTest
    {
        [Fact]
        public void TestMatchSecondTime()
        {
            var server = CreateSimplyRespond(Encoding.ASCII.GetBytes("OK"));
            var proxyServer = CreateSimplyRespond(Encoding.ASCII.GetBytes("OK"));

            ManualResetEvent wait = new ManualResetEvent(false);

            var proxy = new HttpConnectProxy(proxyServer.LocalEndPoint);
            ProxyEventArgs eventArgs = null;
            proxy.Completed += (a, e) =>
            {
                eventArgs = e;
                wait.Set();
            };
            proxy.Connect(server.LocalEndPoint);

            Assert.True(wait.WaitOne(5000));
            Assert.Null(eventArgs.Exception);
            Assert.True(eventArgs.Connected);
        }

        Socket CreateSimplyRespond(byte[] data)
        {
            var socket = NewTcpLocalBound();
            Task.Run(
                () =>
                {
                    var stream = socket.Accept();
                    stream.Send(data);
                    stream.Shutdown(SocketShutdown.Both);
                    socket.Dispose();
                }
            );

            return socket;
        }

        Socket NewTcp() => new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        Socket NewTcpLocalBound()
        {
            var socket = NewTcp();
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            socket.Listen(1);
            return socket;
        }
    }
}
