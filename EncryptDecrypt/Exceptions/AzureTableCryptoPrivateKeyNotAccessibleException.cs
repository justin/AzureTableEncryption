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
    public class AzureTableCryptoNotFoundException : AzureTableCryptoException
    {
        public AzureTableCryptoNotFoundException(int version) :
            base("Could not find a SymmetricKey for EncryptionVersion " + version.ToString(CultureInfo.InvariantCulture))
        {

        }
    }
}
