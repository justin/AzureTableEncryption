using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure;
using System.Globalization;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;

namespace EncryptDecrypt
{
    /// <summary>
    /// Azure TableServiceContext for storing symmetric encryption keys
    /// </summary>
    internal class SymmetricKeyStore
    {
        private CloudStorageAccount storageAccount;

        public SymmetricKeyStore(CloudStorageAccount acct)
        {
            this.storageAccount = acct;
        }
        
        public void SaveSymmetricKey(SymmetricKey sKey)
        {
            var op = TableOperation.InsertOrReplace(sKey);
            var cloudTable = GetTable();
            var result = cloudTable.Execute(op);
        }

        public void DeleteSymmetricKey(SymmetricKey sKey)
        {
            var op = TableOperation.Delete(sKey);
            GetTable().Execute(op);
        }

        public List<SymmetricKey> GetAllKeys()
        {
            var cloudTable = GetTable();

            var query = new TableQuery<SymmetricKey>();

            return cloudTable.ExecuteQuery(query).ToList(); ;
        }

        private CloudTable GetTable()
        {
            return storageAccount.CreateCloudTableClient().GetTableReference("SymmetricKeys");
        }

        public void Create()
        {
            GetTable().CreateIfNotExists();
        }

        public void Delete()
        {
            GetTable().DeleteIfExists();
        }
    }

}
