using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EncryptDecrypt;
using Microsoft.WindowsAzure.Storage;
using NUnit.Framework;

namespace EncryptDecryptTests
{
    public class EncryptableTestEntity : EncryptableTableEntity
    {
        public EncryptableTestEntity()
        {
            this.PartitionKey = "TestEntityPartition";
            this.RowKey = Guid.NewGuid().ToString();
        }

        [Encrypt]
        public string StringField { get; set; }

        [Encrypt]
        public byte[] ByteField { get; set; }

        public void AssertEqualTo(EncryptableTestEntity other)
        {
            Assert.NotNull(other);
            Assert.AreEqual(this.RowKey, other.RowKey, "RowKeys do not match");
            Assert.AreEqual(this.StringField, other.StringField, "String fields do not match");
            CollectionAssert.AreEqual(this.ByteField, other.ByteField, "Byte fields do not match");
        }
    }
}
