using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.Storage.Table.DataServices;

namespace EncryptDecrypt
{
    /// <summary>
    /// Base class for a TableServiceEntity that may contain encrypted properties.
    /// Properties to be encrypted must be marked with the [Encrypt] attribute.
    /// 
    /// This is intended for backwards compatibility with the Azure Storage SDK v1.8 and less. 
    ///
    /// New development should use EncryptableTableEntity by itself (no table base class is required)
    /// </summary>
    public class EncryptableTableServiceEntity : TableServiceEntity
    {
        public int? EncryptionVersion { get; set; }
    }
}
