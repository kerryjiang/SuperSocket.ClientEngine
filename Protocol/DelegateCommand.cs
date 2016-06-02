using System;
using System.Collections.Generic;
using System.Text;
using SuperSocket.ClientEngine;
using SuperSocket.ProtoBase;

namespace SuperSocket.ClientEngine.Protocol
{
    public delegate void CommandDelegate<TClientSession, TPackageInfo>(TClientSession session, TPackageInfo packageInfo);

    class DelegateCommand<TClientSession, TPackageInfo> : ICommand<TClientSession, TPackageInfo>
        where TClientSession : class
        where TPackageInfo : IPackageInfo
    {
        private CommandDelegate<TClientSession, TPackageInfo> m_Execution;

        public DelegateCommand(string name, CommandDelegate<TClientSession, TPackageInfo> execution)
        {
            Name = name;
            m_Execution = execution;
        }

        public void ExecuteCommand(TClientSession session, TPackageInfo packageInfo)
        {
            m_Execution(session, packageInfo);
        }

        public string Name { get; private set; }
    }
}
