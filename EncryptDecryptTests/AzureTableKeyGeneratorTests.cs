using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using EncryptDecrypt;
using System.Security.Cryptography.X509Certificates;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;

namespace EncryptDecryptTests
{
    [TestFixture]
    public class AzureTableKeyGeneratorTests
    {
        const int KEYGEN_TESTS_ENCRYPTION_VERSION = 10000;
        SymmetricKeyStore keysTable;
        AzureTableKeyGenerator keyGen;
        
        [SetUp]
        public void Setup()
        {
            keysTable = new SymmetricKeyStore(CloudStorageAccount.DevelopmentStorageAccount);
            keyGen = new AzureTableKeyGenerator(SetupFixture.TEST_CERT_THUMBPRINT);
        }

        [TearDown]
        public void TearDown()
        {
            keysTable = new SymmetricKeyStore(CloudStorageAccount.DevelopmentStorageAccount);

            //Clear out any leftover test keys
            List<SymmetricKey> keys = keysTable.GetAllKeys();
            SymmetricKey existingKey = keys.FirstOrDefault(k => k.Version == KEYGEN_TESTS_ENCRYPTION_VERSION);
            if (existingKey != null)
            {
                {
                    keysTable.DeleteSymmetricKey(existingKey);
                    keysTable = new SymmetricKeyStore(CloudStorageAccount.DevelopmentStorageAccount);
                }
            }
        }

        [Test]
        public void CreateNewKey()
        {
            keyGen.CreateNewKey(CloudStorageAccount.DevelopmentStorageAccount, KEYGEN_TESTS_ENCRYPTION_VERSION);

            List<SymmetricKey> keys = keysTable.GetAllKeys();
            SymmetricKey key = keys.FirstOrDefault(k => k.Version == KEYGEN_TESTS_ENCRYPTION_VERSION);
            Assert.IsNotNull(key, "Could not find the newly-generated key");
            Assert.AreEqual(SetupFixture.TEST_CERT_THUMBPRINT.Replace(" ", "").ToUpperInvariant(), key.CertificateThumbprint, "Incorrect certificate thumbprint");
            Assert.AreEqual(KEYGEN_TESTS_ENCRYPTION_VERSION, key.Version, "Incorrect encryption version");

            AzureTableCryptoKeyStore keyStore = new AzureTableCryptoKeyStore(CloudStorageAccount.DevelopmentStorageAccount);

            Assert.DoesNotThrow(() =>
            {
                using (var decryptor = keyStore.GetDecryptor(KEYGEN_TESTS_ENCRYPTION_VERSION))
                {
                }
            });
        }
    }
}
