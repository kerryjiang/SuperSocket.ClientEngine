using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Threading;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace SuperSocket.ClientEngine
{
    public class NegotiateStreamTcpSession : AuthenticatedStreamTcpSession
    {
        protected override void StartAuthenticatedStream(Socket client)
        {
            var securityOption = Security;

            if (securityOption == null)
            {
                throw new Exception("securityOption was not configured");
            }

            var stream = new NegotiateStream(new NetworkStream(client));

            var credential = securityOption.Credential;

            if (credential == null)
                credential = (NetworkCredential)CredentialCache.DefaultCredentials;

            Task.Run(async () =>
            {
                try
                {
                    await stream.AuthenticateAsClientAsync(credential, HostName);
                }
                catch(Exception e)
                {
                    EnsureSocketClosed();
                    OnError(e);
                    return;
                }

                OnAuthenticatedStreamConnected(stream);
            });
        }
    }
}
