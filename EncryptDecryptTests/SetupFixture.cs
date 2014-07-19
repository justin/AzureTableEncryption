using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using EncryptDecrypt;
using System.Security.Cryptography.X509Certificates;
using System.Reflection;
using System.IO;
using EncryptDecryptTests;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

[SetUpFixture]
public class SetupFixture
{
    public const string TEST_CERT_THUMBPRINT = "db c8 ae 88 c1 cb 39 60 bb cc 85 28 ed 23 64 a7 fd 4c a3 46";
    public const string TEST_CERT_PFX_NAME = "AzureTableEncryptTestCert.pfx";
    public const int TEST_ENCRYPTION_VERSION = 1000;


    [SetUp]
    public void Setup()
    {
        CloudStorageAccount account = CloudStorageAccount.DevelopmentStorageAccount;

        // Init the Crypto Library.
        AzureTableCrypto.Initialize(account);

        //Make sure the test table exists
        CloudTableClient client = account.CreateCloudTableClient();
        var table = client.GetTableReference(TestTable.TABLE_NAME);
        table.CreateIfNotExists();


        //Check that the test cert is available and installed
        X509Certificate2 cert = CertificateHelper.GetCertificateByThumbprint(TEST_CERT_THUMBPRINT, storeLocation: StoreLocation.LocalMachine, storeName: StoreName.My, requirePrivateKey: true);

        if (cert == null)
        {
            Assert.Fail("The test encryption certificate does not appear to be installed. Before running the tests you must install the certificate by following the instructions in EncryptDecrypTests/InstallingTestCert.txt");
        }

        //Make sure there's an encryption key for us to use
        AzureTableCrypto c = AzureTableCrypto.Get();
        bool encryptionExists = false;
        try
        {
            c.GetDecryptor(TEST_ENCRYPTION_VERSION);
            encryptionExists = true;
        }
        catch (Exception)
        {
        }

        if (!encryptionExists)
        {
            AzureTableKeyGenerator keyGen = new AzureTableKeyGenerator(cert);
            keyGen.CreateNewKey(account, TEST_ENCRYPTION_VERSION);
        }
    }
}
