using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.WindowsAzure;
using System.Data.Services.Client;
using EncryptDecrypt.Exceptions;
using System.Net;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;

namespace EncryptDecrypt
{
    /// <summary>
    /// Store the various encryption keys so that we don't need to load them from storage all the time
    /// </summary>
    /// <remarks>
    /// Is there something we should be doing to secure the memory used by this class?
    /// </remarks>
    internal class AzureTableCryptoKeyStore : IDisposable
    {
        private Dictionary<int, SymmetricAlgorithm> keyCache = new Dictionary<int, SymmetricAlgorithm>();
        internal CloudStorageAccount KeyStoreAccount { get; private set; }

        internal AzureTableCryptoKeyStore(CloudStorageAccount acct)
        {
            this.KeyStoreAccount = acct;

            SymmetricKeyStore keyTable = new SymmetricKeyStore(acct);
            List<SymmetricKey> allKeys = null;

            try
            {
                allKeys = keyTable.GetAllKeys();
            }
            catch (DataServiceQueryException dsq)
            {
                if (dsq.Response.StatusCode == (int)HttpStatusCode.NotFound)
                {
                    //Table hasn't been created, so there aren't any keys. Guess we'll just go with it. 
                    allKeys = new List<SymmetricKey>(0);
                }
                else
                {
                    throw new AzureTableCryptoInitializationException("Failed to load encryption keys from storage", dsq);
                }
            }
            catch (DataServiceClientException dsce)
            {
                if (dsce.StatusCode == (int)HttpStatusCode.NotFound)
                {
                    //Table hasn't been created, so there aren't any keys. Guess we'll just go with it. 
                    allKeys = new List<SymmetricKey>(0);
                }
                else
                {
                    throw new AzureTableCryptoInitializationException("Failed to load encryption keys from storage", dsce);
                }
            }
            catch (Exception ex)
            {
                throw new AzureTableCryptoInitializationException("Could not load encryption keys table", ex);
            }


            foreach (var key in allKeys)
            {
                try
                {
                    X509Certificate2 certificate = CertificateHelper.GetCertificateByThumbprint(key.CertificateThumbprint);
                    if (certificate == null)
                    {
                        //Can't find the cert for this key, just continue
                        continue;
                    }

                    RSACryptoServiceProvider RSA;
                    try
                    {
                        RSA = (RSACryptoServiceProvider)certificate.PrivateKey;
                    }
                    catch (CryptographicException)
                    {
                        throw new AzureTableCryptoPrivateKeyNotAccessibleException(key.Version, key.CertificateThumbprint);
                    }

                    byte[] symmetricCryptoKey = RSA.Decrypt(key.Key, true);

                    AesManaged algorithm = new AesManaged();
                    algorithm.IV = key.iv;
                    algorithm.Key = symmetricCryptoKey;
                    keyCache[key.Version] = algorithm;
                }
                catch (AzureTableCryptoException)
                {
                    //Just rethrow these
                    throw;
                }
                catch (Exception ex)
                {
                    throw new AzureTableCryptoInitializationException("Error initializing crypto key version " + key.Version, ex);
                }
            }
        }

        internal ICryptoTransform GetDecryptor(int version)
        {
            return GetAlgorithm(version).CreateDecryptor();
        }

        internal ICryptoTransform GetEncryptor(int version)
        {
            return GetAlgorithm(version).CreateEncryptor();
        }

        private SymmetricAlgorithm GetAlgorithm(int version)
        {
            SymmetricAlgorithm algo;
            if (!keyCache.TryGetValue(version, out algo))
            {
                throw new AzureTableCryptoNotFoundException(version);
            }
            return algo;
        }

        public void Dispose()
        {
            Dictionary<int, SymmetricAlgorithm> cache = keyCache;
            keyCache = null;

            foreach (var algo in cache.Values)
            {
                algo.Clear();
                algo.Dispose();
            }
        }
    }
}
