using System;
using System.Collections.Generic;
using System.Text;

namespace SuperSocket.ClientEngine
{
    public class SearchMarkState<T>
        where T : IEquatable<T>
    {
        public SearchMarkState(T[] mark)
        {
            Mark = mark;
        }

        public T[] Mark { get; private set; }

        public int Matched { get; set; }
    }
}
