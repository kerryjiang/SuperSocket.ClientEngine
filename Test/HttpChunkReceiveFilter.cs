using System;
using System.Text;
using SuperSocket.ProtoBase;

namespace SuperSocket.ClientEngine.Test
{
    public class HttpChunkReceiveFilter : TerminatorReceiveFilter<HttpPackageInfo>
    {
        private HttpHeaderInfo m_HttpHeader;

        private int m_HeaderSize;

        private StringBuilder m_BodyBuilder;
        public HttpChunkReceiveFilter(HttpHeaderInfo header, int headerSize, StringBuilder bodyBuilder)
            : base(new byte[] { 0x0d, 0x0a })
        {
            m_HttpHeader = header;
            m_HeaderSize = headerSize;
            m_BodyBuilder = bodyBuilder;
        }
        
        public override HttpPackageInfo ResolvePackage(IBufferStream bufferStream)
        {
            var numLen = (int)(bufferStream.Length - m_HeaderSize - 2);

            bufferStream.Skip(m_HeaderSize);

            var chunkSize = 0;

            for (var i = numLen - 1; i >= 0; i--)
            {
                chunkSize = chunkSize + (int)bufferStream.ReadByte() * (16 ^ i);
            }

            if (chunkSize > 0)
            {
                NextReceiveFilter = new HttpChunkDataReceiveFilter(this, m_HeaderSize + numLen + 2 + chunkSize, chunkSize);
                return null;
            }

            // last chunk
            var body = m_BodyBuilder.ToString();
            m_BodyBuilder = null;

            return new HttpPackageInfo("Test", m_HttpHeader, body);
        }

        class HttpChunkDataReceiveFilter : FixedSizeReceiveFilter<HttpPackageInfo>
        {
            HttpChunkReceiveFilter m_ParentFilter;

            int m_Size;

            int m_ChunkSize;

            public HttpChunkDataReceiveFilter(HttpChunkReceiveFilter parentFilter, int totalSize, int chunkSize)
                : base(totalSize)
            {
                m_Size = totalSize;
                m_ChunkSize = chunkSize;
                m_ParentFilter = parentFilter;
            }

            public override HttpPackageInfo ResolvePackage(IBufferStream bufferStream)
            {
                m_ParentFilter.m_BodyBuilder.Append(bufferStream.Skip(m_Size - m_ChunkSize).ReadString(this.m_ChunkSize, Encoding.UTF8));
                NextReceiveFilter =  m_ParentFilter;
                return null;
            }
        }
    }
}
