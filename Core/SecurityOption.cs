using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SuperSocket.ClientEngine
{
    /// <summary>
    /// Security options
    /// </summary>
    public class SecurityOption
    {
        /// <summary>
        /// The SslProtocols want to be enabled
        /// </summary>
        public SslProtocols EnabledSslProtocols { get; set; }

        /// <summary>
        /// Client X509 certificates
        /// </summary>
        public X509CertificateCollection Certificates { get; set; }

        /// <summary>
        /// Whether allow untrusted certificate
        /// </summary>
        public bool AllowUnstrustedCertificate { get; set; }

        /// <summary>
        /// Whether allow the certificate whose name doesn't match current remote endpoint's host name
        /// </summary>
        public bool AllowNameMismatchCertificate { get; set; }

        /// <summary>
        /// Whether allow the certificate chain errors
        /// </summary>
        public bool AllowCertificateChainErrors { get; set; }


        public NetworkCredential Credential { get; set; }


        public SecurityOption()
            : this(GetDefaultProtocol(), new X509CertificateCollection())
        {

        }

        public SecurityOption(SslProtocols enabledSslProtocols)
            : this(enabledSslProtocols, new X509CertificateCollection())
        {
            
        }

        public SecurityOption(SslProtocols enabledSslProtocols, X509Certificate certificate)
            : this(enabledSslProtocols, new X509CertificateCollection(new X509Certificate[] { certificate }))
        {

        }

        public SecurityOption(SslProtocols enabledSslProtocols, X509CertificateCollection certificates)
        {
            EnabledSslProtocols = enabledSslProtocols;
            Certificates = certificates;
        }

        public SecurityOption(NetworkCredential credential)
        {
            Credential = credential;
        }
        
        private static SslProtocols GetDefaultProtocol()
        {
#if NETSTANDARD
            return SslProtocols.Tls11 | SslProtocols.Tls12;
#else
            return SslProtocols.Default;
#endif
        }
    }
}
