using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperSocket.ProtoBase;

namespace SuperSocket.ClientEngine
{
    public class PackageEventArgs<TPackageInfo> : EventArgs
        where TPackageInfo : IPackageInfo
    {
        public TPackageInfo Package { get; private set; }

        public PackageEventArgs(TPackageInfo package)
        {
            Package = package;
        }
    }
}
