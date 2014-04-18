using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.IO;
using Microsoft.WindowsAzure;
using System.Data.Services.Client;
using System.Reflection;
using System.Xml.Linq;
using System.Threading;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using EncryptDecrypt.Exceptions;

namespace EncryptDecrypt
{
    /// <summary>
    /// Handles the grunt work of encrypting/decrypting bytes. Also initializes the KeyStore if necessary
    /// </summary>
    public class AzureTableCrypto
    {
        private static AzureTableCryptoKeyStore keyStore;
        private static AzureTableCrypto singleton = new AzureTableCrypto();

        [Obsolete("Only here for backwards compatibility. Use the static Get() method instead.")]
        public AzureTableCrypto(CloudStorageAccount acct)
        {
            //This is here just for backwards compatibility
            if (keyStore == null)
            {
                Initialize(acct);
            }
        }

        internal AzureTableCrypto()
        {
        }

        /// <summary>
        /// Initializes the crypto by reading symmetric keys from the specified storage account. 
        /// This should be called once while the app is starting up. 
        /// A WorkerRole.OnStart() method or the Global.Application_Start() function would be a good place for that. 
        /// </summary>
        /// <param name="acct"></param>
        public static void Initialize(CloudStorageAccount acct)
        {
            //This is basically allowing only one keystore. First configured store wins. That could be annoying if you wanted to have multiple keystores,
            //but it's easier if you have multiple storage accounts and want them all to use the same keystore.
            //I figure the latter case is more likely, and it's the one I want anyway. 
            if (keyStore == null)
            {
                keyStore = new AzureTableCryptoKeyStore(acct);
            }
        }

        /// <summary>
        /// Get an instance. Be sure to call Initialize() first.
        /// </summary>
        /// <returns></returns>
        public static AzureTableCrypto Get()
        {
            if (keyStore == null)
            {
                throw new AzureTableCryptoInitializationException("The key store has not been initialized");
            }

            return singleton;
        }

        /// <summary>
        /// Reload the key store from storage. eg, if a new certificate and/or symmetric key has been created, this will pick up those changes. 
        /// Note, you should try to avoid calling this too much, as it does not currently Dispose() of resources deterministically. 
        /// </summary>
        internal static void ReloadKeyStore(CloudStorageAccount acct)
        {
            AzureTableCryptoKeyStore newKeyStore = new AzureTableCryptoKeyStore(acct);
            AzureTableCryptoKeyStore oldKeyStore = Interlocked.Exchange(ref keyStore, newKeyStore);
            if (oldKeyStore != null)
            {
                //Crap, this isn't threadsafe at all - others could have a reference to the keystore or the objects in it
                //For the moment I'm going to just let the GC do all the disposal for us, as there shouldn't be too much and this shouldn't happen too much 
                //oldKeyStore.Dispose();
            }
        }
        
        /// <summary>
        /// Encrypt a string, apply Base64 encoding to the resulting bytes, and return that string
        /// </summary>
        public string EncryptStringAndBase64(int version, string s)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(s);
            byte[] cryptedBytes = Encrypt(version, bytes);
            return Convert.ToBase64String(cryptedBytes);
        }

        /// <summary>
        /// Takes a Base64 encoded string, turns it into bytes and decrypts the bytes into a string
        /// </summary>
        public string DecryptStringFromBase64(int version, string base64String)
        {
            byte[] bytes = Decrypt(version, Convert.FromBase64String(base64String));
            return Encoding.Unicode.GetString(bytes);
        }

        /// <summary>
        /// Encrypt a byte array
        /// </summary>
        public byte[] Encrypt(int encryptionVersion, byte[] bytes)
        {
            using (MemoryStream msEncrypted = new MemoryStream())
            {
                using (var encryptor = keyStore.GetEncryptor(encryptionVersion))
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypted, encryptor, CryptoStreamMode.Write))
                    {
                        using (MemoryStream inStream = new MemoryStream(bytes))
                        {
                            inStream.CopyTo(csEncrypt);
                        }
                        csEncrypt.Close();
                    }
                    return msEncrypted.ToArray();
                }
            }
        }

        /// <summary>
        /// Decrypt a byte array
        /// </summary>
        public byte[] Decrypt(int encryptionVersion, byte[] bytes)
        {
            using (var decryptor = keyStore.GetDecryptor(encryptionVersion))
            {
                using (MemoryStream msDecrypted = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msDecrypted, decryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(bytes, 0, bytes.Length);
                    }
                    return msDecrypted.ToArray();
                }
            }
        }

        /// <summary>
        /// Get an Encryptor for the specified encryption version. 
        /// Callers are responsible for Dispose()'ing the returned transform
        /// </summary>
        /// <param name="encryptionVersion"></param>
        /// <returns></returns>
        public ICryptoTransform GetEncryptor(int encryptionVersion)
        {
            return keyStore.GetEncryptor(encryptionVersion);
        }

        /// <summary>
        /// Get a Decryptor for the specified encryption version. 
        /// Callers are responsible for Dispose()'ing the returned transform
        /// </summary>
        /// <param name="encryptionVersion"></param>
        /// <returns></returns>
        public ICryptoTransform GetDecryptor(int encryptionVersion)
        {
            return keyStore.GetDecryptor(encryptionVersion);
        }

        /// <summary>
        /// Decrypt all properties on an object that are marked with the Encrypt attribute
        /// </summary>
        public void DecryptObject(int encryptionVersion, object e)
        {
            foreach (PropertyInfo property in e.GetType().GetProperties())
            {
                try
                {
                    object[] transparentEncryptAttributes = property.GetCustomAttributes(typeof(EncryptAttribute), false);
                    if (transparentEncryptAttributes.Length > 0)
                    {
                        if (property.PropertyType == typeof(string))
                        {
                            string propertyValue = (string)property.GetValue(e, null);
                            if (!string.IsNullOrEmpty(propertyValue))
                            {
                                propertyValue = DecryptStringFromBase64(encryptionVersion, propertyValue);
                                property.SetValue(e, propertyValue, null);
                            }
                        }
                        else if (property.PropertyType == typeof(byte[]))
                        {
                            byte[] propertyValue = (byte[])property.GetValue(e, null);
                            if (propertyValue != null && propertyValue.Length > 0)
                            {
                                propertyValue = Decrypt(encryptionVersion, propertyValue);
                                property.SetValue(e, propertyValue, null);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new DecryptionException(e, property.Name, encryptionVersion, ex);
                }
            }
        }

        /// <summary>
        /// Encrypt all properties on an object that are marked with the Encrypt attribute
        /// </summary>
        /// <param name="encryptionVersion"></param>
        /// <param name="e"></param>
        public void EncryptObject(int encryptionVersion, object e)
        {
            foreach (PropertyInfo property in e.GetType().GetProperties())
            {
                object[] transparentEncryptAttributes = property.GetCustomAttributes(typeof(EncryptAttribute), false);
                if (transparentEncryptAttributes.Length > 0 && property.PropertyType == typeof(string))
                {
                    string propertyValue = (string)property.GetValue(e, null);
                    if (!string.IsNullOrEmpty(propertyValue))
                    {
                        string cryptedString = EncryptStringAndBase64(encryptionVersion, propertyValue);
                        property.SetValue(e, cryptedString, null);
                    }
                }
            }
        }
    }

}
