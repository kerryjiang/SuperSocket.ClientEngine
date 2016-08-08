using System;
using System.Text;
using SuperSocket.ProtoBase;

namespace SuperSocket.ClientEngine.Test
{
    public class HttpBodyReceiveFilter : FixedSizeReceiveFilter<HttpPackageInfo>
    {
        private HttpHeaderInfo m_HttpHeader;
        
        private int m_HeaderSize;
        
        private int m_Size;
        
        public HttpBodyReceiveFilter(HttpHeaderInfo httpHeader, int size, int headerSize)
            : base(size)
        {
            m_HttpHeader = httpHeader;
            m_Size = size;
            m_HeaderSize = headerSize;
        }

        public override HttpPackageInfo ResolvePackage(IBufferStream bufferStream)
        {
            var total = (int)bufferStream.Length;
            return new HttpPackageInfo("Test", m_HttpHeader, bufferStream.Skip(m_HeaderSize).ReadString(total - m_HeaderSize, Encoding.UTF8));
        }
    }
}
