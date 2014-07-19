using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EncryptDecrypt;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using NUnit.Framework;

namespace EncryptDecryptTests
{
    [TestFixture]
    public class EncryptableTableEntityTests
    {
        CloudStorageAccount acct = CloudStorageAccount.DevelopmentStorageAccount;
        CloudTableClient client;
        CloudTable table;
        EncryptableTestEntity testEntity;


        [SetUp]
        public void Setup()
        {
            client = acct.CreateCloudTableClient();
            table = client.GetTableReference("AzureTableCryptoUnitTestTable");

            testEntity = new EncryptableTestEntity()
            {
                StringField = Guid.NewGuid().ToString(),
                ByteField = Guid.NewGuid().ToByteArray(),
                EncryptionVersion = SetupFixture.TEST_ENCRYPTION_VERSION 
            };
        }

        [Test]
        public void CreateReadUpdateDelete()
        {
            table.Execute(TableOperation.Insert(testEntity));

            try
            {
                var q = new TableQuery<EncryptableTestEntity>().Where(
                        TableQuery.CombineFilters(
                            TableQuery.GenerateFilterCondition("PartitionKey", "eq", testEntity.PartitionKey),
                            "and",
                            TableQuery.GenerateFilterCondition("RowKey", "eq", testEntity.RowKey)
                        )
                    )
                    .Take(1);

                EncryptableTestEntity retrievedEntity = table.ExecuteQuery(q).FirstOrDefault();
                testEntity.AssertEqualTo(retrievedEntity);

                testEntity.StringField = Guid.NewGuid().ToString();
                testEntity.ByteField = Guid.NewGuid().ToByteArray();

                table.Execute(TableOperation.Replace(testEntity));

                var secondRetrievedEntity = table.ExecuteQuery(q).FirstOrDefault();
                testEntity.AssertEqualTo(secondRetrievedEntity);
            }
            finally
            {
                testEntity.ETag = "*";
                table.Execute(TableOperation.Delete(testEntity));
            }
        }

        [Test]
        public void CreateReadDelete_NullValues()
        {
            testEntity.StringField = null;
            testEntity.ByteField = null;

            table.Execute(TableOperation.Insert(testEntity));

            try
            {
                var q = new TableQuery<EncryptableTestEntity>().Where(
                            TableQuery.CombineFilters(
                                TableQuery.GenerateFilterCondition("PartitionKey", "eq", testEntity.PartitionKey),
                                "and",
                                TableQuery.GenerateFilterCondition("RowKey", "eq", testEntity.RowKey)
                            )
                        )
                        .Take(1);

                EncryptableTestEntity retrievedEntity = table.ExecuteQuery(q).FirstOrDefault();
                testEntity.AssertEqualTo(retrievedEntity);
            }
            finally
            {
                testEntity.ETag = "*";
                table.Execute(TableOperation.Delete(testEntity));
            }
        }

        [Test]
        public void CreateReadDelete_EmptyValues()
        {
            testEntity.StringField = "";
            testEntity.ByteField = new byte[0];

            try
            {
                table.Execute(TableOperation.Insert(testEntity));

                var q = new TableQuery<EncryptableTestEntity>().Where(
                            TableQuery.CombineFilters(
                                TableQuery.GenerateFilterCondition("PartitionKey", "eq", testEntity.PartitionKey),
                                "and",
                                TableQuery.GenerateFilterCondition("RowKey", "eq", testEntity.RowKey)
                            )
                        )
                        .Take(1);

                EncryptableTestEntity retrievedEntity = table.ExecuteQuery(q).FirstOrDefault();
                testEntity.AssertEqualTo(retrievedEntity);
            }
            finally
            {
                testEntity.ETag = "*";
                table.Execute(TableOperation.Delete(testEntity));
            }
        }

        [Test]
        public void EncryptionUsed()
        {
            //Write encrypted, but read unencrypted, so the values should be different
            table.Execute(TableOperation.Insert(testEntity));

            var q = new TableQuery().Where(
                        TableQuery.CombineFilters(
                            TableQuery.GenerateFilterCondition("PartitionKey", "eq", testEntity.PartitionKey),
                            "and",
                            TableQuery.GenerateFilterCondition("RowKey", "eq", testEntity.RowKey)
                        )
                    ).Take(1);

            var plaintextEntity = table.ExecuteQuery(q).FirstOrDefault();
            Assert.AreNotEqual(testEntity.StringField, plaintextEntity.Properties["StringField"].StringValue);
            CollectionAssert.AreNotEqual(testEntity.ByteField, plaintextEntity.Properties["ByteField"].BinaryValue);
        }

        [Test]
        public void ReadWithEncryptionDisabled()
        {
            //Test that if we set EncryptionVersion to 0, the entity can still be read
            table.Execute(TableOperation.Insert(testEntity));

            var q = new TableQuery<ReadUnencryptedEncryptableTestEntity>().Where(
                        TableQuery.CombineFilters(
                            TableQuery.GenerateFilterCondition("PartitionKey", "eq", testEntity.PartitionKey),
                            "and",
                            TableQuery.GenerateFilterCondition("RowKey", "eq", testEntity.RowKey)
                        )
                    ).Take(1);

            var plaintextEntity = table.ExecuteQuery(q).FirstOrDefault();
            Assert.AreNotEqual(plaintextEntity.StringField, testEntity.StringField);

        }
    }
}
