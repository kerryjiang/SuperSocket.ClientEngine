using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SuperSocket.ClientEngine.Base
{
    public class ProxyException : Exception
    {
        public ProxyException(string message)
            : base(message)
        {

        }

        public ProxyException(string message, Exception socketException)
            : base(message, socketException)
        {

        }
    }
}
