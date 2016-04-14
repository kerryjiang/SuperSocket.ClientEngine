using System;
using System.Collections.Generic;
using System.Text;
using SuperSocket.ProtoBase;

namespace SuperSocket.ClientEngine.Protocol
{
    public interface ICommand
    {
        string Name { get; }
    }

    public interface ICommand<TSession, TPackageInfo> : ICommand
        where TPackageInfo : IPackageInfo
        where TSession : IClientSession
    {
        void ExecuteCommand(TSession session, TPackageInfo packageInfo);
    }
}
