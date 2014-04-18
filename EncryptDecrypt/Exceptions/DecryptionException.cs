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
    public class DecryptionException : AzureTableCryptoException
    {
        public object Entity { get; private set; }

        public DecryptionException(object entity, string propertyName, int encryptionVersion, Exception inner)
            : base("Error decrypting property \"" + propertyName + "\" on entity of type " + entity.GetType().Name + " with EncryptionVersion " + encryptionVersion, inner)
        {
            this.Entity = entity;
        }
    }
}
