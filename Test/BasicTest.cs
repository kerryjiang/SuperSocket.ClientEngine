using System.Threading.Tasks;
using Xunit;
using System.Net;
using SuperSocket.ClientEngine;

namespace SuperSocket.ClientEngine.Test
{
    public class BasicTest
    {
        [Fact]
        public async Task TestConnection()
        {
            var client = new EasyClient();
            
            var ret = await client.ConnectAsync(new DnsEndPoint("github.com", 433));
            
            Assert.True(ret);
        }
    }
}
