using System.Threading.Tasks;
using Xunit;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using SuperSocket.ProtoBase;
using System.Text;

namespace SuperSocket.ClientEngine.Test
{
    public class SearchMarkTest
    {
        [Fact]
        public void TestMatchSecondTime()
        {
            byte[] first = Encoding.ASCII.GetBytes("HTTP/1.0 200 Connection Established\r\n");
            byte[] second = Encoding.ASCII.GetBytes("\r\n");

            byte[] mark = Encoding.ASCII.GetBytes("\r\n\r\n");

            // -2 means "\r\n\r\n" is partially matched at last 2 bytes
            Assert.Equal(-2, first.SearchMark(0, first.Length, mark));

            // 0 means later part of "\r\n\r\n" is fully matched at 0
            Assert.Equal(0, second.SearchMark(0, second.Length, mark, 2));
        }

        [Fact]
        public void TestMatchFirstTime()
        {
            byte[] first = Encoding.ASCII.GetBytes("HTTP/1.0 200 Connection Established\r\n\r\n");

            byte[] mark = Encoding.ASCII.GetBytes("\r\n\r\n");

            // "\r\n\r\n" is matched at first[35] to [38]
            Assert.Equal(35, first.SearchMark(0, first.Length, mark));
        }

        [Fact]
        public void TestMatchFirstTimeWithSearchMarkState()
        {
            byte[] first = Encoding.ASCII.GetBytes("HTTP/1.0 200 Connection Established\r\n\r\n");

            byte[] mark = Encoding.ASCII.GetBytes("\r\n\r\n");

            var searchState = new SearchMarkState<byte>(mark);

            // "\r\n\r\n" is matched at first[35] to [38]
            Assert.Equal(35, first.SearchMark(0, first.Length, searchState));
        }

        [Fact]
        public void TestMatchSecondTimeWithSearchMarkState()
        {
            byte[] first = Encoding.ASCII.GetBytes("HTTP/1.0 200 Connection Established\r\n");
            byte[] second = Encoding.ASCII.GetBytes("\r\n");

            byte[] mark = Encoding.ASCII.GetBytes("\r\n\r\n");

            var searchState = new SearchMarkState<byte>(mark);

            {
                // -1 means: not matched, or partially matched.
                Assert.Equal(-1, first.SearchMark(0, first.Length, searchState));

                // Check if (1 <= searchState.Matched) in case of partial match.
            }
            {
                var prevMatched = searchState.Matched;
                Assert.Equal(prevMatched, 2);

                // "\r\n\r\n" is completely matched on second buffer at second[0] to [1].
                Assert.Equal(0, second.SearchMark(0, second.Length, searchState));
            }
        }
    }
}
