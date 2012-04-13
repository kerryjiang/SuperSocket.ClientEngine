using System;
using System.Collections.Generic;
using System.Text;

namespace SuperSocket.ClientEngine.Protocol
{
    public class BinaryCommandInfo : CommandInfo<byte[]>
    {
        public BinaryCommandInfo(string key, byte[] data)
            : base(key, data)
        {

        }
    }
}
