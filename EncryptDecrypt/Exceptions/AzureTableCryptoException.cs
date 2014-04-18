using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EncryptDecrypt.Exceptions
{
    /// <summary>
    /// Base class for Azure Table Crypto exceptions
    /// </summary>
    public class AzureTableCryptoException : Exception
    {
        public AzureTableCryptoException()
            : base()
        {
        }

        public AzureTableCryptoException(string msg)
            : base(msg)
        {
        }

        public AzureTableCryptoException(string msg, Exception inner)
            : base(msg, inner)
        {
        }
    }
}
