using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EncryptDecrypt;
using Microsoft.WindowsAzure.Storage;
using NUnit.Framework;

namespace EncryptDecryptTests
{
    /// <summary>
    /// Simple table for unit testing
    /// </summary>
    internal class TestTable : EncryptableTableBase
    {

        public TestTable()
            : this(true)
        {
        }

        public TestTable(bool cryptoEnabled)
            : base(CloudStorageAccount.DevelopmentStorageAccount, cryptoEnabled)
        {
            base.IgnoreResourceNotFoundException = true;
            base.MergeOption = System.Data.Services.Client.MergeOption.NoTracking;
        }


        public const string TABLE_NAME = "AzureTableCryptoUnitTestTable";

        public IQueryable<TestEntity> TestEntities
        {
            get
            {
                return this.CreateQuery<TestEntity>(TABLE_NAME);
            }
        }

        public TestEntity GetEntity(TestEntity requestedEntity)
        {
            return (from e in TestEntities
                    where e.PartitionKey == requestedEntity.PartitionKey
                    && e.RowKey == requestedEntity.RowKey
                    select e).FirstOrDefault();
        }

        public void AddEntity(TestEntity entity)
        {
            TestEntity existingRow = GetExistingRow(entity.PartitionKey, entity.RowKey);
            if (existingRow != null)
            {
                base.Detach(existingRow);
            }

            this.AddObject(TABLE_NAME, entity);
            this.SaveChangesWithRetries();
        }

        public void UpdateEntity(TestEntity entity)
        {
            TestEntity existingRow = GetExistingRow(entity.PartitionKey, entity.RowKey);
            if (existingRow != null)
            {
                base.Detach(existingRow);
            }

            this.AttachTo(TABLE_NAME, entity, "*");
            base.UpdateObject(entity);
            this.SaveChangesWithRetries();
        }

        private TestEntity GetExistingRow(string partitionKey, string rowKey)
        {
            var query = (from e in base.Entities
                         where e.Entity is TestEntity
                         && ((TestEntity)e.Entity).RowKey == rowKey
                         && ((TestEntity)e.Entity).PartitionKey == partitionKey
                         select (TestEntity)e.Entity);

            return query.FirstOrDefault();
        }

        public void DeleteEntity(TestEntity entity)
        {
            this.DeleteObject(entity);
            this.SaveChangesWithRetries();
        }

        /// <summary>
        /// Encryption to use - allows us to enable/disable encryption for testing purposes
        /// </summary>
        protected override int? EncryptionVersion
        {
            get
            {
                return CurrentEncryptionVersion;
            }
        }

        public int CurrentEncryptionVersion
        {
            get;
            set;
        }
    }

    internal class TestEntity : EncryptableTableServiceEntity
    {
        public TestEntity()
        {
            this.PartitionKey = "TestEntityPartition";
            this.RowKey = Guid.NewGuid().ToString();
        }

        [Encrypt]
        public string StringField { get; set; }

        [Encrypt]
        public byte[] ByteField { get; set; }

        public void AssertEqualTo(TestEntity other)
        {
            Assert.NotNull(other);
            Assert.AreEqual(this.RowKey, other.RowKey, "RowKeys do not match");
            Assert.AreEqual(this.StringField, other.StringField, "String fields do not match");
            CollectionAssert.AreEqual(this.ByteField, other.ByteField, "Byte fields do not match");
        }
    }
}
