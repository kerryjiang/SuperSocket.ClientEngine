using System;
using System.Net;

namespace SuperSocket.ClientEngine
{
    public interface IProxyConnector
    {
        void Connect(EndPoint remoteEndPoint);

        event EventHandler<ProxyEventArgs> Completed;
    }
}