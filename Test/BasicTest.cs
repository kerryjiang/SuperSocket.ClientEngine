using System.Threading.Tasks;
using Xunit;
using System.Net;
using System.Net.Sockets;

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
    }
}
