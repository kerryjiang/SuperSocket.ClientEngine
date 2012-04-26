using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;

namespace SuperSocket.ClientEngine
{
    public interface IClientSession
    {
        IProxyConnector Proxy { get; set; }

        int ReceiveBufferSize { get; set; }

        bool IsConnected { get; }

        void Connect();

        void Send(byte[] data, int offset, int length);

        void Send(IList<ArraySegment<byte>> segments);

        void Close();

        event EventHandler Connected;

        event EventHandler Closed;

        event EventHandler<ErrorEventArgs> Error;

        event EventHandler<DataEventArgs> DataReceived;
    }
}
