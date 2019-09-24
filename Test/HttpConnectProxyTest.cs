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
        public void TestHttp10SimpleOnePacket()
        {
            SimulateHttpConnectProxy(
                simulateResponseFromProxyServer: proxyServerPeer =>
                {
                    SendLine(proxyServerPeer, "HTTP/1.0 200 Connection Established\r\n\r\n");
                },
                verifyCompletedEvent: e =>
                {
                    Assert.Null(e.Exception);
                    Assert.True(e.Connected);
                },
                testCopyDataFromLeftToRight: SendAndReceiveHello
            );
        }

        [Fact]
        public void TestHttp11SimpleOnePacket()
        {
            SimulateHttpConnectProxy(
                simulateResponseFromProxyServer: proxyServerPeer =>
                {
                    SendLine(proxyServerPeer, "HTTP/1.1 200 Connection Established\r\n\r\n");
                },
                verifyCompletedEvent: e =>
                {
                    Assert.Null(e.Exception);
                    Assert.True(e.Connected);
                },
                testCopyDataFromLeftToRight: SendAndReceiveHello
            );
        }

        [Fact]
        public void TestHttp11ComplexOnePacket()
        {
            SimulateHttpConnectProxy(
                simulateResponseFromProxyServer: proxyServerPeer =>
                {
                    SendLine(proxyServerPeer,
                        "HTTP/1.1 200 Connection Established\r\n" +
                        "Proxy-agent: Apache/2.2.29 (Win32)\r\n" +
                        "\r\n"
                    );
                },
                verifyCompletedEvent: e =>
                {
                    Assert.Null(e.Exception);
                    Assert.True(e.Connected);
                },
                testCopyDataFromLeftToRight: SendAndReceiveHello
            );
        }

        [Fact]
        public void TestHttp11ComplexMultiPacket()
        {
            SimulateHttpConnectProxy(
                simulateResponseFromProxyServer: proxyServerPeer =>
                {
                    // Actual response simulation from: Apache/2.2.29 (Win32)
                    SendLine(proxyServerPeer, "HTTP/1.1 200 Connection Established\r\n");

                    // This leads to "System.Exception: protocol error: more data has been received"
                    SendLine(proxyServerPeer, "Proxy-agent: Apache/2.2.29 (Win32)\r\n\r\n");
                },
                verifyCompletedEvent: e =>
                {
                    Assert.Null(e.Exception);
                    Assert.True(e.Connected);
                },
                testCopyDataFromLeftToRight: SendAndReceiveHello
            );
        }

        [Fact]
        public void TestHttp11ComplexOnePacketAsForbidden()
        {
            SimulateHttpConnectProxy(
                simulateResponseFromProxyServer: proxyServerPeer =>
                {
                    // Actual 403 response simulation from: Apache/2.2.29 (Win32)

                    // This leads to "System.Exception: protocol error: more data has been received"
                    SendLine(proxyServerPeer,
                        string.Join("\r\n",
                            "HTTP/1.1 403 Forbidden",
                            "Date: Tue, 24 Sep 2019 04:35:48 GMT",
                            "Content-Length: 216",
                            "Content-Type: text/html; charset=iso-8859-1",
                            "",
                            "<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML 2.0//EN\">",
                            "<html><head>",
                            "<title>403 Forbidden</title>",
                            "</head><body>",
                            "<h1>Forbidden</h1>",
                            "<p>You don't have permission to access 192.168.2.181:7",
                            "on this server.</p>",
                            "</body></html>"
                        )
                    );
                },
                verifyCompletedEvent: e =>
                {
                    // This pattern is: NOT SUPPORTED FOR NOW!
                    Assert.NotNull(e.Exception);
                    Assert.Equal("protocol error: more data has been received", e.Exception.Message);
                    Assert.False(e.Connected);
                },
                testCopyDataFromLeftToRight: SendAndReceiveHello
            );
        }


        void SimulateHttpConnectProxy(
            Action<Socket> simulateResponseFromProxyServer,
            Action<ProxyEventArgs> verifyCompletedEvent,
            Action<Socket, Socket> testCopyDataFromLeftToRight
        )
        {
            var server = NewTcpPeer();
            var proxyServer = NewTcpPeer();

            var awaitAtProxyServer = proxyServer.AcceptAsync();

            var proxy = new HttpConnectProxy(proxyServer.LocalEndPoint, 1024, null);
            var eventArgs = (ProxyEventArgs)null;
            var eventPulled = new ManualResetEvent(false);
            proxy.Completed += (sender, e) =>
            {
                eventArgs = e;
                eventPulled.Set();
            };

            proxy.Connect(server.LocalEndPoint);

            var proxyServerPeer = awaitAtProxyServer.GetAwaiter().GetResult();
            Assert.Equal($"CONNECT 127.0.0.1:{((IPEndPoint)server.LocalEndPoint).Port} HTTP/1.1\r\n", ReadLine(proxyServerPeer));
            Assert.Equal($"Host: 127.0.0.1:{((IPEndPoint)server.LocalEndPoint).Port}\r\n", ReadLine(proxyServerPeer));
            Assert.Equal($"Proxy-Connection: Keep-Alive\r\n", ReadLine(proxyServerPeer));
            Assert.Equal($"\r\n", ReadLine(proxyServerPeer));

            simulateResponseFromProxyServer(proxyServerPeer);

            Assert.True(eventPulled.WaitOne(5000));

            // This verification needs to be ran on xUnit thread.
            // Otherwise xUnit cannot identify which test is failure.
            verifyCompletedEvent(eventArgs);

            if (eventArgs.Connected)
            {
                testCopyDataFromLeftToRight?.Invoke(proxyServerPeer, eventArgs.Socket);
            }
        }

        void SendAndReceiveHello(Socket left, Socket right)
        {
            var echoMessage = $"HELLO, it is {DateTime.Now.Ticks} now!\r\n";

            SendLine(left, echoMessage);
            Assert.Equal(echoMessage, ReadLine(right));
        }

        void SendLine(Socket socket, string line)
        {
            Thread.Sleep(100);
            socket.Send(Encoding.ASCII.GetBytes(line));
        }

        string ReadLine(Socket socket)
        {
            byte[] lineBuff = new byte[1024];
            int at = 0;
            while (true)
            {
                int received = socket.Receive(lineBuff, at, 1, SocketFlags.None);
                if (received < 0)
                {
                    break;
                }
                if (lineBuff[at] == 10)
                {
                    at++;
                    break;
                }
                at++;
            }
            return Encoding.ASCII.GetString(lineBuff, 0, at);
        }

        Socket NewTcpPeer()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            socket.Listen(1);
            return socket;
        }
    }
}
