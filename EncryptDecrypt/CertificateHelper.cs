using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography.X509Certificates;

namespace EncryptDecrypt
{
    /// <summary>
    /// A few methods to help manage X509 certificates
    /// </summary>
    public static class CertificateHelper
    {
        /// <summary>
        /// Find a certificate with the given thumbprint
        /// </summary>
        /// <param name="thumbprint">Thumbprint to search for</param>
        /// <param name="storeLocation">The CertificateStore to search. Some stores require admin access</param>
        /// <param name="storeName">The StoreName to search under. Generally "My" is the correct one to use</param>
        /// <param name="requirePrivateKey">Only return certificates with a PrivateKey embedded</param>
        /// <returns></returns>
        public static X509Certificate2 GetCertificateByThumbprint(string thumbprint, StoreLocation storeLocation = StoreLocation.LocalMachine, StoreName storeName = StoreName.My, bool requirePrivateKey = false)
        {
            if (string.IsNullOrEmpty(thumbprint))
            {
                throw new ArgumentNullException("thumbprint");
            }

            thumbprint = thumbprint.Replace(" ", "").ToUpperInvariant();

            X509Store store = new X509Store(storeName, storeLocation);

            try
            {
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);

                X509Certificate2Collection foundCerts = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
                foreach (var cert in foundCerts)
                {
                    if (!requirePrivateKey || cert.HasPrivateKey)
                    {
                        return cert;
                    }
                }
            }
            finally
            {
                store.Close();
            }

            return null;
        }

        /// <summary>
        /// Find a certificate with the given common name. This is an imprecise search method, as multiple certificates may have the same name. 
        /// Returns the first certificate found matching the specified name. 
        /// </summary>
        /// <param name="storeLocation">The CertificateStore to search. Some stores require admin access</param>
        /// <param name="storeName">The StoreName to search under. Generally "My" is the correct one to use</param>
        /// <param name="requirePrivateKey">Only return certificates with a PrivateKey embedded</param>
        /// <returns></returns>
        public static X509Certificate2 GetCertificateByCommonName(string commonName, StoreLocation storeLocation = StoreLocation.LocalMachine, StoreName storeName = StoreName.My, bool requirePrivateKey = false)
        {
            if (string.IsNullOrEmpty(commonName))
            {
                throw new ArgumentNullException("commonName");
            }

            //commonName = commonName.Replace(" ", "").ToUpperInvariant();

            X509Store store = new X509Store(storeName, storeLocation);

            try
            {
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);

                X509Certificate2Collection foundCerts = store.Certificates.Find(X509FindType.FindBySubjectName, commonName, false);
                foreach (var cert in foundCerts)
                {
                    if (!requirePrivateKey || cert.HasPrivateKey)
                    {
                        return cert;
                    }
                }
            }
            finally
            {
                store.Close();
            }

            return null;
        }

        /// <summary>
        /// Create a new X509Certificate2 using default encryption and thumbprinting
        /// </summary>
        public static X509Certificate2 CreateCertificate(string distinguishedName = "CN=AzureTableEncryption", DateTime? validityStart = null, DateTime? validityEnd = null, string password = null)
        {
            validityStart = validityStart ?? DateTime.UtcNow;
            validityEnd = validityEnd ?? DateTime.UtcNow.AddYears(30);

            byte[] certBytes = CertificateCreator.CreateSelfSignCertificatePfx(distinguishedName, validityStart.Value, validityEnd.Value, password);
            return new X509Certificate2(certBytes, "", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
        }

        /// <summary>
        /// Delete the specified certificate from the certificate store
        /// </summary>
        public static void DeleteCertificate(X509Certificate2 cert, StoreLocation storeLocation = StoreLocation.LocalMachine, StoreName storeName = StoreName.My)
        {
            if (cert == null)
            {
                throw new ArgumentNullException("cert");
            }

            X509Store store = new X509Store(storeName, storeLocation);
            store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);

            try
            {
                store.Remove(cert);
            }
            finally
            {
                store.Close();
            }
        }

        /// <summary>
        /// Install a certificate into the certificate store
        /// </summary>
        public static void InstallCertificate(X509Certificate2 cert, StoreLocation storeLocation = StoreLocation.LocalMachine, StoreName storeName = StoreName.My)
        {
            if (cert == null)
            {
                throw new ArgumentNullException("cert");
            }

            X509Store store = new X509Store(storeName, storeLocation);
            store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            
            try
            {
                store.Add(cert);
            }
            finally
            {
                store.Close();
            }
        }
    }
}
