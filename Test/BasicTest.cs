using System.Threading.Tasks;
using Xunit;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using SuperSocket.ProtoBase;

namespace SuperSocket.ClientEngine.Test
{
    public class BasicTest
    {
        [Fact]
        public async Task TestConnection()
        {
            var client = new EasyClient();
            
            client.Initialize(new FakeReceiveFilter(), (p) =>
            {
                // do nothing
            });
            
            var ret = await client.ConnectAsync(new DnsEndPoint("github.com", 443));
            
            Assert.True(ret);
        }
        
        [Fact]
        public async Task TestConnectRepeat()
        {
            var client = new EasyClient();
            
            client.Initialize(new FakeReceiveFilter(), (p) =>
            {
                // do nothing
            });

            client.Error += (s, e) =>
            {
                Console.WriteLine("Error:" + e.Exception.Message);
            };
            
            Console.WriteLine("Connecting");
            var ret = await client.ConnectAsync(new DnsEndPoint("github.com", 443));
            Console.WriteLine("Connected");
            Assert.True(ret);
            
            Console.WriteLine("Closing");
            await client.Close();
            Console.WriteLine("Closed");
            
            Console.WriteLine("Connecting");
            ret = await client.ConnectAsync(new DnsEndPoint("github.com", 443));
            Console.WriteLine("Connected");
            
            Assert.True(ret);
            
            Console.WriteLine("Closing");
            await client.Close();
            Console.WriteLine("Closed");
        }

        [Fact]
        public void TestArraySegmentList()
        {
            var list = new BufferList();

            list.Add(new ArraySegment<byte>(new byte[1024], 0, 100));
            list.Add(new ArraySegment<byte>(new byte[1024], 2, 200));
            list.Add(new ArraySegment<byte>(new byte[1024], 3, 300));

            var lastOne = list.Last;

            Assert.Equal(3, lastOne.Offset);
            Assert.Equal(300, lastOne.Count);            
        }
    }
}
