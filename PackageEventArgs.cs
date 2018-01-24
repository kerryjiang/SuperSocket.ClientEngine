using System;

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