using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EncryptDecrypt;
using Microsoft.WindowsAzure.Storage;
using NUnit.Framework;
using Microsoft.WindowsAzure.Storage.Table;
using EncryptDecrypt.Exceptions;

namespace EncryptDecryptTests
{
    /// <summary>
    /// Simple table for unit testing
    /// </summary>
    internal class TestTable : EncryptableTableEntity
    {
        private CloudStorageAccount storageAccount;

        public TestTable(CloudStorageAccount _account)
        {
            this.storageAccount = _account;
        }

        public const string TABLE_NAME = "AzureTableCryptoUnitTestTable";

        public EncryptableTestEntity GetEntity(EncryptableTestEntity requestedEntity)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<EncryptableTestEntity>(requestedEntity.PartitionKey, requestedEntity.RowKey);
            TableResult retrievedResult = GetTable().Execute(retrieveOperation);
            EncryptableTestEntity entity = (EncryptableTestEntity)retrievedResult.Result;

            return entity;

        }

        public void AddEntity(EncryptableTestEntity entity)
        {
            try
            {
                var op = TableOperation.InsertOrReplace(entity);
                GetTable().Execute(op);
            }    
            catch (Exception ex)
            {
                // No Op.
            }
        }

        public void UpdateEntity(EncryptableTestEntity entity)
        {
            var op = TableOperation.InsertOrReplace(entity);
            GetTable().Execute(op);
        }

        private EncryptableTestEntity GetExistingRow(string partitionKey, string rowKey)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<EncryptableTestEntity>(partitionKey, rowKey);
            TableResult retrievedResult = GetTable().Execute(retrieveOperation);
            EncryptableTestEntity entity = (EncryptableTestEntity)retrievedResult.Result;

            return entity;
        }

        public void DeleteEntity(EncryptableTestEntity entity)
        {
            entity.ETag = "*";
            var op = TableOperation.Delete(entity);
            GetTable().Execute(op);
        }

        /// <summary>
        /// Encryption to use - allows us to enable/disable encryption for testing purposes
        /// </summary>
        public override int? EncryptionVersion
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

        private CloudTable GetTable()
        {
            return storageAccount.CreateCloudTableClient().GetTableReference(TABLE_NAME);
        }
    }
}
