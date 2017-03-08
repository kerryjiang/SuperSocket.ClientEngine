using System;
using System.Text;
using SuperSocket.ProtoBase;

namespace SuperSocket.ClientEngine.Test
{
    public class HttpChunkReceiveFilter : TerminatorReceiveFilter<HttpPackageInfo>
    {
        private HttpHeaderInfo m_HttpHeader;
        private StringBuilder m_BodyBuilder;
        public HttpChunkReceiveFilter(HttpHeaderInfo header, StringBuilder bodyBuilder)
            : base(new byte[] { 0x0d, 0x0a })
        {
            m_HttpHeader = header;
            m_BodyBuilder = bodyBuilder;
        }
        
        public override HttpPackageInfo ResolvePackage(IBufferStream bufferStream)
        {
            var numLen = (int)(bufferStream.Length - 2);
            var chunkSize = 0;

            var chunksizeString = bufferStream.ReadString(numLen, Encoding.ASCII);
            Console.WriteLine("ChunkSizeStr:" + chunksizeString);
            chunkSize = Convert.ToInt32(chunksizeString, 16);

            Console.WriteLine($"ChunkSize: {chunkSize}");
            bufferStream.Buffers.Clear();
            NextReceiveFilter = new HttpChunkDataReceiveFilter(this, chunkSize + 2);
            return null;
        }

        class HttpChunkDataReceiveFilter : FixedSizeReceiveFilter<HttpPackageInfo>
        {
            HttpChunkReceiveFilter m_ParentFilter;

            public HttpChunkDataReceiveFilter(HttpChunkReceiveFilter parentFilter, int chunkSize)
                : base(chunkSize)
            {
                m_ParentFilter = parentFilter;
            }

            public override HttpPackageInfo Filter(BufferList data, out int rest)
            {
                // get the previous length for comparing later
                var prevLen = m_ParentFilter.m_BodyBuilder.Length;

                var package = base.Filter(data, out rest);

                var currentLen = m_ParentFilter.m_BodyBuilder.Length;

                // if there is no data chunk parsed, no reset should be return, otherwise return the rest directly
                if (currentLen == prevLen)
                    rest = 0;

                return package;
            }

            public override HttpPackageInfo ResolvePackage(IBufferStream bufferStream)
            {
                var realChunkSize = this.Size - 2;

                if (realChunkSize == 0)
                {
                    // last chunk
                    var body = m_ParentFilter.m_BodyBuilder.ToString();
                    return new HttpPackageInfo("Test", m_ParentFilter.m_HttpHeader, body);
                }

                m_ParentFilter.m_BodyBuilder.Append(bufferStream.ReadString(realChunkSize, Encoding.UTF8));
                Console.WriteLine("Part:" + m_ParentFilter.m_BodyBuilder.ToString());
                NextReceiveFilter =  m_ParentFilter;
                bufferStream.Buffers.Clear();

                return null;
            }
        }
    }
}
