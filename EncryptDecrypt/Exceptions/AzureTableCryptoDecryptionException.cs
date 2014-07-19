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
        public EncryptableTableEntity Entity { get; private set; }

        public AzureTableCryptoDecryptionException(EncryptableTableEntity entity)
            : this(null, "Error decrypting a table entity")
        {
        }

        public AzureTableCryptoDecryptionException(EncryptableTableEntity entity, string msg)
            : this(entity, msg, null)
        {
        }

        public AzureTableCryptoDecryptionException(EncryptableTableEntity entity, string msg, Exception inner)
            : base(msg, inner)
        {
            this.Entity = entity;
        }
    }
}
