using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Table.DataServices;

namespace EncryptDecrypt.Exceptions
{
    /// <summary>
    /// Indicates an error while decrypting an entity
    /// </summary>
    public class EncryptionException : AzureTableCryptoException
    {
        public TableEntity Entity { get; private set; }

        public EncryptionException(TableEntity entity, string propertyName, Exception inner)
            : base("Error encrypting property \"" + propertyName + "\" on entity of type " + entity.GetType().Name, inner)
        {
            this.Entity = entity;
        }
    }
}
