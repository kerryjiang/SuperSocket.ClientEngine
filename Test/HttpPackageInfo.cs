using SuperSocket.ProtoBase;

namespace SuperSocket.ClientEngine.Test
{
    public class HttpPackageInfo : HttpPackageInfoBase<string>
    {
        public HttpPackageInfo(string key, HttpHeaderInfo header, string body)
            : base(key, header, body)
        {
            
        }
    }
}
