using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using EncryptDecrypt.Exceptions;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Security.Cryptography;
using System.Diagnostics;

namespace EncryptDecrypt
{
    /// <summary>
    /// An entity base class supporting transparent encryption.
    /// This entity does not require a Table base class, but you must set the encryption version.
    /// 
    /// You must also initialize the crypto by calling AzureTableCrypto.Initialize() once before using this class. 
    /// A WorkerRole.OnStart() method or the Global.Application_Start() function would be a good place for that. 
    /// </summary>
    public class EncryptableTableEntity : TableEntity
    {
        public virtual int? EncryptionVersion { get; set; }

        public override void ReadEntity(IDictionary<string,EntityProperty> properties, OperationContext operationContext)
        {
            base.ReadEntity(properties, operationContext);

            try
            {
                if ((this.EncryptionVersion.HasValue && this.EncryptionVersion.Value > 0))
                {
                    AzureTableCrypto.Get().DecryptObject(this.EncryptionVersion.Value, this);
                }
            }
            catch (FormatException fe)
            {
                //FormatException gets thrown when the data is not properly Base-64 encoded

                throw new AzureTableCryptoDecryptionException(this, "Error decrypting table service entity", fe);
            }
            catch (CryptographicException ce)
            {
                throw new AzureTableCryptoDecryptionException(this, "Error decrypting table service entity", ce);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Read the entity without decrypting any of the encrypted fields. Useful so that you can work with encrypted entites without having the decryption certificates installed locally.
        /// </summary>
        protected void ReadEntityUnencrypted(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            base.ReadEntity(properties, operationContext);
        }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            //We need to encrypt the appropriate fields
            //But we don't want to alter the original object
            //So use the regular base class to serialize the properties first, then modify those
 
            var properties = base.WriteEntity(operationContext);

            if ((this.EncryptionVersion.HasValue && this.EncryptionVersion > 0))
            {
                foreach (PropertyInfo property in this.GetType().GetProperties())
                {
                    try
                    {
                        object[] encryptionAttrs = property.GetCustomAttributes(typeof(EncryptAttribute), false);
                        EntityProperty serializedProperty;

                        if (encryptionAttrs.Any() && properties.TryGetValue(property.Name, out serializedProperty))
                        {
                            if (serializedProperty.PropertyType == EdmType.Binary)
                            {
                                if (serializedProperty.BinaryValue != null && serializedProperty.BinaryValue.Length > 0)
                                {
                                    serializedProperty.BinaryValue = AzureTableCrypto.Get().Encrypt(this.EncryptionVersion.Value, serializedProperty.BinaryValue);
                                }
                            }
                            else if (serializedProperty.PropertyType == EdmType.String)
                            {
                                if (!string.IsNullOrEmpty(serializedProperty.StringValue))
                                {
                                    serializedProperty.StringValue = AzureTableCrypto.Get().EncryptStringAndBase64(this.EncryptionVersion.Value, serializedProperty.StringValue);
                                }
                            }
                            else
                            {
                                throw new AzureTableCryptoException("Don't know how to encrypt properties of type " + serializedProperty.PropertyType);
                            }
                        }
                    }
                    catch (EncryptionException ex)
                    {
                        Trace.Write(ex.ToString());
                    }
                    catch (Exception ex)
                    {
                        throw new EncryptionException(this, property.Name, ex);
                    }
                }
            }


            return properties;
        }
    }
}
