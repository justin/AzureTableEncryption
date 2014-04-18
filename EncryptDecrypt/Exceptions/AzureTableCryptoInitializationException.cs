using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EncryptDecrypt.Exceptions
{
    /// <summary>
    /// Indicates an error initialization the crypto keys.
    /// </summary>
    public class AzureTableCryptoInitializationException : AzureTableCryptoException
    {
        public AzureTableCryptoInitializationException()
            : base()
        {
        }

        public AzureTableCryptoInitializationException(string msg)
            : base(msg)
        {
        }

        public AzureTableCryptoInitializationException(string msg, Exception inner)
            : base(msg, inner)
        {
        }
    }
}
