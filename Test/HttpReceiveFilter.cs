using SuperSocket.ProtoBase;

namespace SuperSocket.ClientEngine.Test
{
    public class HttpReceiveFilter : HttpHeaderReceiveFilterBase<HttpPackageInfo>
    {
        protected override IReceiveFilter<HttpPackageInfo> GetBodyReceiveFilter(HttpHeaderInfo header, int headerSize)
        {
            var contentLength = int.Parse(header["Content-Length"]);
            var totalLength = headerSize + contentLength;
            return new HttpBodyReceiveFilter(header, totalLength, headerSize);
        }

        protected override HttpPackageInfo ResolveHttpPackageWithoutBody(HttpHeaderInfo header)
        {
            return new HttpPackageInfo("Test", header, string.Empty);
        }
    }
}
