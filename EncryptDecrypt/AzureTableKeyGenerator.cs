using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Table.Protocol;

namespace EncryptDecrypt
{
    /// <summary>
    /// Class for generating and storing new symmetric encryption keys. 
    /// Keys are created, encrypted with an X509 certificate, then placed in Azure table storage
    /// </summary>
    public class AzureTableKeyGenerator
    {
        private X509Certificate2 cert;

        public AzureTableKeyGenerator(string thumbprint, StoreLocation storeLocation = StoreLocation.LocalMachine, StoreName storeName = StoreName.My)
        {
            this.cert = CertificateHelper.GetCertificateByThumbprint(thumbprint, storeLocation: storeLocation, storeName: storeName);
        }

        public AzureTableKeyGenerator(X509Certificate2 cert)
        {
            this.cert = cert;
        }

        /// <summary>
        /// Create a new symmetric key, encrypt it with the X509Certificate already supplied, and upload it to the SymmetricKeys table in the specified StorageAccount.
        /// Note you should be careful to not call this frequently - it is intended for offline/manual use or occasional testing. 
        /// </summary>
        /// <param name="storageAccount"></param>
        /// <param name="versionNumber"></param>
        public void CreateNewKey(CloudStorageAccount storageAccount, int versionNumber)
        {
            //Create the key
            SymmetricKey newKeySet = CreateNewAESSymmetricKeyset();
            newKeySet.Version = versionNumber;

            //Create the table
            (new SymmetricKeyStore(storageAccount)).Create();
            
            //Save the new row
            SymmetricKeyStore ctx = new SymmetricKeyStore(storageAccount);
            ctx.SaveSymmetricKey(newKeySet);

            AzureTableCrypto.ReloadKeyStore(storageAccount);
        }

        /// <summary>
        /// Creates a symmetric key.  See this link for more information behind the numbers
        /// http://blogs.msdn.com/b/shawnfa/archive/2006/10/09/the-differences-between-rijndael-and-aes.aspx
        /// </summary>
        /// <returns></returns>
        private SymmetricKey CreateNewAESSymmetricKeyset()
        {
            if (cert == null)
            {
                throw new InvalidOperationException("Unable to create new AES keyset; Certificate not loaded.");
            }

            byte[] symmKey, iv;

            using (AesManaged aes = new AesManaged())
            {
                aes.GenerateIV();
                aes.GenerateKey();

                symmKey = aes.Key;
                iv = aes.IV;

                aes.Clear();
            }

            // Encrypt the Symmetric Key for storage
            byte[] encryptedKey = EncryptRSA(symmKey, cert);

            SymmetricKey symmKeySet = new SymmetricKey() { 
                iv = iv, 
                Key = encryptedKey,
                CertificateThumbprint = cert.Thumbprint
            };
            
            return symmKeySet;
        }

        private byte[] DecryptRSA(byte[] b, X509Certificate2 cert)
        {
            RSACryptoServiceProvider RSA = (RSACryptoServiceProvider)cert.PrivateKey;
            byte[] decrypt = RSA.Decrypt(b, true);
            return decrypt;
        }

        private byte[] EncryptRSA(byte[] b, X509Certificate2 cert)
        {
            RSACryptoServiceProvider RSA = (RSACryptoServiceProvider)cert.PublicKey.Key;
            var encrypt = RSA.Encrypt(b, true);
            return encrypt;
        }
    }
}
