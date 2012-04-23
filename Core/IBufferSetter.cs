using System;
using System.Collections.Generic;
using System.Text;

namespace SuperSocket.ClientEngine
{
    public interface IBufferSetter
    {
        void SetBuffer(ArraySegment<byte> bufferSegment);
    }
}
