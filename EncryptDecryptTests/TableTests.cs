using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using EncryptDecrypt.Exceptions;

namespace EncryptDecryptTests
{
    [TestFixture]
    public class TableTests
    {
        TestTable encryptedTable;
        TestTable unencryptedTable;
        TestEntity testEntity;


        [SetUp]
        public void Setup()
        {
            encryptedTable = new TestTable(true);
            encryptedTable.CurrentEncryptionVersion = SetupFixture.TEST_ENCRYPTION_VERSION;

            unencryptedTable = new TestTable(false);
            unencryptedTable.CurrentEncryptionVersion = 0;

            testEntity = new TestEntity()
            {
                StringField = Guid.NewGuid().ToString(),
                ByteField = Guid.NewGuid().ToByteArray()
            };
        }

        [Test]
        public void CreateReadUpdateDelete()
        {
            try
            {
                encryptedTable.AddEntity(testEntity);

                TestEntity retrievedEntity = encryptedTable.GetEntity(testEntity);
                testEntity.AssertEqualTo(retrievedEntity);

                testEntity.StringField = Guid.NewGuid().ToString();
                testEntity.ByteField = Guid.NewGuid().ToByteArray();
                encryptedTable.UpdateEntity(testEntity);

                TestEntity secondRetrievedEntity = encryptedTable.GetEntity(testEntity);
                testEntity.AssertEqualTo(secondRetrievedEntity);
            }
            finally
            {
                encryptedTable.DeleteEntity(testEntity);
                Assert.IsNull(encryptedTable.GetEntity(testEntity));
            }
        }

        [Test]
        public void EncryptionUsed()
        {
            //Write encrypted, but read unencrypted, so the values should be different
            encryptedTable.AddEntity(testEntity);

            TestEntity plaintextEntity = unencryptedTable.GetEntity(testEntity);
            Assert.AreNotEqual(testEntity.StringField, plaintextEntity.StringField);
            CollectionAssert.AreNotEqual(testEntity.ByteField, plaintextEntity.ByteField);
        }

        [Test]
        public void EncryptionBroken()
        {
            //Test that an improperly-encrypted entity throws the expected exception
            testEntity.EncryptionVersion = 1; //just totally lying here

            unencryptedTable.AddEntity(testEntity);
            unencryptedTable.SaveChanges();

            var exception = Assert.Throws<AzureTableCryptoDecryptionException>(() =>
            {
                encryptedTable.GetEntity(testEntity);
            });

            Assert.NotNull(exception.Entity, "Should have set the entity in our exception");
            Assert.AreEqual(testEntity.PartitionKey, exception.Entity.PartitionKey);
            Assert.AreEqual(testEntity.RowKey, exception.Entity.RowKey);
        }
    }
}
