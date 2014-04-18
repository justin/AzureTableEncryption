using System;
using System.Globalization;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Table.DataServices;

namespace EncryptDecrypt
{
    /// <summary>
    /// Storage for a symmetric key. 
    /// </summary>
    public class SymmetricKey : TableEntity
    {
        public SymmetricKey()
        {
            this.PartitionKey = "SymmetricKey";
            CreateDate = DateTime.UtcNow;
        }

        public byte[] Key { get; set; }
        public byte[] iv { get; set; }

        public string CertificateThumbprint { get; set; }

        /// <summary>
        /// A version number assigned by the app using this library. New versions could use a different encryption algorithm, or just a new key for the same algorithm.
        /// </summary>
        public int Version
        {
            get
            {
                return Convert.ToInt32(base.RowKey);
            }
            set
            {
                base.RowKey = value.ToString(CultureInfo.InvariantCulture);
            }
        }

        public DateTime CreateDate { get; set; }

    }
}
