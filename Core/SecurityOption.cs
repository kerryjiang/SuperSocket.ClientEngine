using System;
using System.Collections.Generic;
using System.Linq;
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

        public SecurityOption()
        {
            EnabledSslProtocols = SslProtocols.Default;
            Certificates = new X509CertificateCollection();
        }

        public SecurityOption(SslProtocols enabledSslProtocols)
            : this(enabledSslProtocols, new X509CertificateCollection())
        {
            
        }

        public SecurityOption(SslProtocols enabledSslProtocols, X509CertificateCollection certificates)
        {
            EnabledSslProtocols = enabledSslProtocols;
            Certificates = certificates;
        }

        public SecurityOption(SslProtocols enabledSslProtocols, X509Certificate certificate)
        {
            EnabledSslProtocols = enabledSslProtocols;
            Certificates = new X509CertificateCollection(new X509Certificate[] { certificate });
        }
    }
}
