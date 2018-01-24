using System;

namespace SuperSocket.ClientEngine
{
    public interface IBufferSetter
    {
        void SetBuffer(ArraySegment<byte> bufferSegment);
    }
}