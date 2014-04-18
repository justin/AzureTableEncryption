using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using EncryptDecrypt;
using System.Security.Cryptography.X509Certificates;

namespace EncryptDecryptTests
{
    [TestFixture]
    public class CertificateHelperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void CreateNewCertificateTest()
        {
            X509Certificate2 newCert = CertificateHelper.CreateCertificate(distinguishedName: "CN=EncryptDecryptTests.CertificateHelperTests.CreateNewCertificateTest");
            Assert.NotNull(newCert);
            Assert.True(newCert.HasPrivateKey);
        }

        [Test]
        public void NewCertificate_CreateInstallRetrieveDelete()
        {
            X509Certificate2 newCert = CertificateHelper.CreateCertificate(distinguishedName: "CN=EncryptDecryptTests.CertificateHelperTests.CreateNewCertificateTest");
            try
            {
                Assert.NotNull(newCert);
                Assert.True(newCert.HasPrivateKey);

                CertificateHelper.InstallCertificate(newCert, storeLocation: StoreLocation.LocalMachine, storeName: StoreName.My);

                //Retrieve the created cert
                X509Certificate2 retrievedCert = CertificateHelper.GetCertificateByThumbprint(newCert.Thumbprint, storeLocation:StoreLocation.LocalMachine, storeName: StoreName.My);
                Assert.NotNull(retrievedCert);
                Assert.True(retrievedCert.HasPrivateKey);
                Assert.NotNull(retrievedCert.PrivateKey);
                Assert.AreEqual(newCert.Thumbprint, retrievedCert.Thumbprint);

                //Delete the cert
                CertificateHelper.DeleteCertificate(retrievedCert, storeLocation: StoreLocation.LocalMachine, storeName: StoreName.My);

                //Should not be able to find the cert now
                Assert.IsNull(CertificateHelper.GetCertificateByThumbprint(newCert.Thumbprint, storeLocation: StoreLocation.LocalMachine, storeName: StoreName.My));
                newCert = null;
            }
            finally
            {
                if (newCert != null)
                {
                    CertificateHelper.DeleteCertificate(newCert);
                }
            }
        }

        [Test]
        public void FindCertificateByName()
        {
            string certName = "EncryptDecryptTests.CertificateHelperTests.CreateNewCertificateTest." + Guid.NewGuid().ToString();
            X509Certificate2 newCert = CertificateHelper.CreateCertificate(distinguishedName: "CN=" + certName);
            try
            {
                Assert.NotNull(newCert);
                Assert.True(newCert.HasPrivateKey);

                CertificateHelper.InstallCertificate(newCert, storeLocation: StoreLocation.LocalMachine, storeName: StoreName.My);

                //Retrieve the created cert
                X509Certificate2 retrievedCert = CertificateHelper.GetCertificateByCommonName(certName, storeLocation: StoreLocation.LocalMachine, storeName: StoreName.My);
                Assert.NotNull(retrievedCert);
                Assert.AreEqual(newCert.Thumbprint, retrievedCert.Thumbprint);
            }
            finally
            {
                if (newCert != null)
                {
                    CertificateHelper.DeleteCertificate(newCert);
                }
            }
        }
    }
}
