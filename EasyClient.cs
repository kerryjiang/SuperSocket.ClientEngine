using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperSocket.ProtoBase;
using System.Net;

namespace SuperSocket.ClientEngine
{
    public class EasyClient : EasyClientBase
    {
        private Action<IPackageInfo> m_Handler;

        public void Initialize<TPackageInfo>(IReceiveFilter<TPackageInfo> receiveFilter, Action<TPackageInfo> handler)
            where TPackageInfo : IPackageInfo
        {
            PipeLineProcessor = new DefaultPipelineProcessor<TPackageInfo>(receiveFilter);
            m_Handler = (p) => handler((TPackageInfo)p);
        }

        protected override void HandlePackage(IPackageInfo package)
        {
            var handler = m_Handler;

            if (handler == null)
                return;

            handler(package);
        }
    }

    public class EasyClient<TPackageInfo> : EasyClientBase
        where TPackageInfo : IPackageInfo
    {
        public event EventHandler<PackageEventArgs<TPackageInfo>> NewPackageReceived;

        public EasyClient()
        {
            
        }

        public virtual void Initialize(IReceiveFilter<TPackageInfo> receiveFilter)
        {
            PipeLineProcessor = new DefaultPipelineProcessor<TPackageInfo>(receiveFilter);
        }

        protected override void HandlePackage(IPackageInfo package)
        {
            var handler = NewPackageReceived;

            if (handler == null)
                return;

            handler(this, new PackageEventArgs<TPackageInfo>((TPackageInfo)package));
        }
    }
}
