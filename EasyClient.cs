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
        public void Initialize<TPackageInfo>(IReceiveFilter<TPackageInfo> receiveFilter, Action<TPackageInfo> handler)
            where TPackageInfo : IPackageInfo
        {
            PipeLineProcessor = new DefaultPipelineProcessor<TPackageInfo>(new PackageHandlerWrap<TPackageInfo>(handler), receiveFilter);
        }

        class PackageHandlerWrap<TPakcageInfo> : IPackageHandler<TPakcageInfo>
            where TPakcageInfo : IPackageInfo
        {
            private Action<TPakcageInfo> m_Handler;

            public PackageHandlerWrap(Action<TPakcageInfo> handler)
            {
                m_Handler = handler;
            }

            public void Handle(TPakcageInfo package)
            {
                m_Handler(package);
            }
        }
    }

    public class EasyClient<TPackageInfo> : EasyClientBase, IPackageHandler<TPackageInfo>
        where TPackageInfo : IPackageInfo
    {
        public event EventHandler<PackageEventArgs<TPackageInfo>> NewPackageReceived;

        public EasyClient()
        {
            
        }

        public virtual void Initialize(IReceiveFilter<TPackageInfo> receiveFilter)
        {
            PipeLineProcessor = new DefaultPipelineProcessor<TPackageInfo>(this, receiveFilter);
        }

        public void Handle(TPackageInfo package)
        {
            var handler = NewPackageReceived;

            if (handler == null)
                return;

            handler(this, new PackageEventArgs<TPackageInfo>(package));
        }
    }
}
