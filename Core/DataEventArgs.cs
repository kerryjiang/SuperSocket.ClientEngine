using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SuperSocket.ClientEngine.Core
{
    public class DataEventArgs : EventArgs
    {
        public byte[] Data { get; set; }

        public int Offset { get; set; }

        public int Length { get; set; }
    }
}
