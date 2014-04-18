using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EncryptDecrypt;
using Microsoft.WindowsAzure.Storage;
using NUnit.Framework;

namespace EncryptDecryptTests
{
    public class ReadUnencryptedEncryptableTestEntity : EncryptableTestEntity
    {
        public override void ReadEntity(IDictionary<string, Microsoft.WindowsAzure.Storage.Table.EntityProperty> properties, OperationContext operationContext)
        {
            base.ReadEntityUnencrypted(properties, operationContext);
        }
    }
}
