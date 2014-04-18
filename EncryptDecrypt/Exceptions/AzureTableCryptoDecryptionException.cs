using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.Storage.Table.DataServices;

namespace EncryptDecrypt.Exceptions
{
    /// <summary>
    /// Indicates an error while decrypting an entity
    /// </summary>
    public class AzureTableCryptoDecryptionException : AzureTableCryptoException
    {
        public TableServiceEntity Entity { get; private set; }

        public AzureTableCryptoDecryptionException(TableServiceEntity entity)
            : this(null, "Error decrypting a table service entity")
        {
        }

        public AzureTableCryptoDecryptionException(TableServiceEntity entity, string msg)
            : this(entity, msg, null)
        {
        }

        public AzureTableCryptoDecryptionException(TableServiceEntity entity, string msg, Exception inner)
            : base(msg, inner)
        {
            this.Entity = entity;
        }
    }
}
