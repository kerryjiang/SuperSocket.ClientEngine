using SuperSocket.ProtoBase;

namespace SuperSocket.ClientEngine.Test
{
    public class FakeReceiveFilter : TerminatorReceiveFilter<StringPackageInfo>
    {
        public FakeReceiveFilter()
            : base(new byte[] { 0x01, 0x02 })
        {
            
        }
        
        public override StringPackageInfo ResolvePackage(IBufferStream bufferStream)
        {
            return null;
        }
    }
}
