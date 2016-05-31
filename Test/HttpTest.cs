using System.Threading.Tasks;
using Xunit;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;

namespace SuperSocket.ClientEngine.Test
{
    public class HttpTest
    {
        [Fact]
        public async Task TestGet()
        {
            var client = new EasyClient();
            
            client.Initialize(new HttpReceiveFilter(), (p) =>
            {
                Console.WriteLine(p.Body);
            });
            
            var ret = await client.ConnectAsync(new DnsEndPoint("github.com", 443));
            
            Assert.True(ret);
            
            var sb = new StringBuilder();
            
            sb.AppendLine("HTTP 1.1/GET");
            
            var data = Encoding.ASCII.GetBytes(sb.ToString());
            
            client.Send(new ArraySegment<byte>(data, 0, data.Length));
        }
    }
}
