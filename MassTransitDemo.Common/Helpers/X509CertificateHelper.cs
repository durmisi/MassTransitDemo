

using System.Security.Cryptography.X509Certificates;

namespace MassTransitDemo.Common.Helpers
{
    public static class X509CertificateHelper
    {
        public static X509Certificate2? GetCertificate(StoreName storeName, StoreLocation storeLocation, string sSLThumbprint)
        {
            X509Certificate2? x509Certificate = null;

            X509Store store = new X509Store(storeName, storeLocation);
            store.Open(OpenFlags.ReadOnly);

            try
            {
                X509Certificate2Collection certificatesInStore = store.Certificates;

                x509Certificate = certificatesInStore.OfType<X509Certificate2>()
                    .FirstOrDefault(cert =>
                    {
                        return cert != null && string.Equals(
                               cert.Thumbprint?.ToLower(),
                               sSLThumbprint?.ToLower(),
                               StringComparison.OrdinalIgnoreCase
                           );
                    });
            }
            finally
            {
                store.Close();
            }

            return x509Certificate;
        }
    }
}
