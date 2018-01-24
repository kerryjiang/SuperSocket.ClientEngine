using System;

namespace SuperSocket.ClientEngine
{
    public class ErrorEventArgs : EventArgs
    {
        public Exception Exception { get; private set; }

        public ErrorEventArgs(Exception exception)
        {
            Exception = exception;
        }
    }
}