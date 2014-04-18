using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace EncryptDecrypt.Exceptions
{
    /// <summary>
    /// Indicates that the requested crypto version was not found
    /// </summary>
    public class AzureTableCryptoPrivateKeyNotAccessibleException : AzureTableCryptoException
    {
        public AzureTableCryptoPrivateKeyNotAccessibleException(int encryptionVersion, string thumbprint) :
            base("Could not load RSA private key for EncryptionVersion " + encryptionVersion.ToString(CultureInfo.InvariantCulture) + " from certificate with thumbprint " + thumbprint)
        {
        }
    }
}
