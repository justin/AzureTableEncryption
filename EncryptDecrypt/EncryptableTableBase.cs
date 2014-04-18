using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Services.Client;
using Microsoft.WindowsAzure;
using System.Xml.Linq;
using System.Reflection;
using System.Globalization;
using Microsoft.WindowsAzure.Storage.Table.DataServices;
using Microsoft.WindowsAzure.Storage;
using System.Security.Cryptography;
using EncryptDecrypt.Exceptions;

namespace EncryptDecrypt
{
    /// <summary>
    /// Base class for a TableServiceContext that may contain encrypted entities.
    /// Entities that will be encrypted must inherit from EncryptableTableServiceEntity and have attributes marked up with the [Encrypt] attribute
    /// This is intended for backwards compatibility with the Azure Storage SDK v1.8 and less. 
    ///
    /// New development should use EncryptableTableEntity by itself (no table base class is required)
    /// </summary>
    public abstract class EncryptableTableBase : TableServiceContext
    {
        protected AzureTableCrypto TableCrypto { get; private set; }
        
        /// <summary>
        /// Get the desired encryption version to use. Entities that are added or updated will be written with this version.
        /// Entities may be read using any version that is still available in the table.
        /// If this property returns null, then both encryption and decryption will be disabled
        /// </summary>
        protected abstract int? EncryptionVersion { get; }

        public EncryptableTableBase(CloudStorageAccount storageAccount)
            : this(storageAccount, true)
        {

        }

        /// <summary>
        /// Construct an EncryptableTableBase
        /// </summary>
        /// <param name="storageAccount"></param>
        /// <param name="cryptoEnabled">
        /// Determines whether encryption/decryption as a whole is enabled or not. 
        /// If this is set to false rows will be neither encrypted nor decrypted. This can allow you to work with any unencrypted fields in an entity,
        /// as long as you don't mess with the encrypted ones. 
        /// </param>
        public EncryptableTableBase(CloudStorageAccount storageAccount, bool cryptoEnabled)
            : base(storageAccount.CreateCloudTableClient())
        {
            if (cryptoEnabled)
            {
                AzureTableCrypto.Initialize(storageAccount);
                this.TableCrypto = AzureTableCrypto.Get();
                this.ReadingEntity += new EventHandler<ReadingWritingEntityEventArgs>(DataServiceContextEx_ReadingEntity);
                this.WritingEntity += new EventHandler<ReadingWritingEntityEventArgs>(DataServiceContextEx_WritingEntity);
            }
        }

        private void DataServiceContextEx_ReadingEntity(object sender, ReadingWritingEntityEventArgs e)
        {
            EncryptableTableServiceEntity entity = e.Entity as EncryptableTableServiceEntity;
            try
            {
            if (entity != null && entity.EncryptionVersion.HasValue && entity.EncryptionVersion.Value > 0)
            {
                this.TableCrypto.DecryptObject(entity.EncryptionVersion.Value, entity);
            }
            }
            catch (FormatException fe)
            {
                //FormatException gets thrown when the data is not properly Base-64 encoded

                throw new AzureTableCryptoDecryptionException(entity, "Error decrypting table service entity", fe);
            }
            catch (CryptographicException ce)
            {
                throw new AzureTableCryptoDecryptionException(entity, "Error decrypting table service entity", ce);
            }
        }


        private static readonly XNamespace metadataNamespace = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
        private static readonly XNamespace dataServicesNamespace = "http://schemas.microsoft.com/ado/2007/08/dataservices";
        private static readonly XName xnEntityProperties = metadataNamespace + "properties";
        private static readonly XName encryptionVersionElementName = dataServicesNamespace + "EncryptionVersion";
        private static readonly XName nullAttributeName = metadataNamespace + "null";
        private static readonly XName dataTypeName = metadataNamespace + "type";
                
        private void DataServiceContextEx_WritingEntity(object sender, ReadingWritingEntityEventArgs e)
        {
            EncryptableTableServiceEntity entity = e.Entity as EncryptableTableServiceEntity;
            if (entity != null)
            {
                entity.EncryptionVersion = this.EncryptionVersion;
                
                // e.Data gives you the XElement for the Serialization of the Entity 
                //Using XLinq, you can add/Remove properties to the element Payload  
                XElement xePayload = e.Data.Descendants(xnEntityProperties).FirstOrDefault();

                //Set the EncryptionVersion element
                if (xePayload != null)
                {
                    //Get the Property of the entity you want to encrypt on the server
                    XElement xVersionElement = xePayload.Descendants(encryptionVersionElementName).First();
                    if (this.EncryptionVersion == null)
                    {
                        xVersionElement.Remove();
                    }
                    else
                    {
                        xVersionElement.Value = this.EncryptionVersion.Value.ToString(CultureInfo.InvariantCulture);

                        //If the EncyptedVersion element had the "null" attribute applied to it, then remove that.
                        xVersionElement.Attributes(nullAttributeName).Remove();
                    }
                }

                if (this.EncryptionVersion.HasValue && this.EncryptionVersion > 0)
                {
                    foreach (PropertyInfo property in e.Entity.GetType().GetProperties())
                    {
                        object[] transparentEncryptAttributes = property.GetCustomAttributes(typeof(EncryptAttribute), false);
                        if (transparentEncryptAttributes.Length > 0)
                        {
                            //The XName of the property we are going to encrypt from the payload
                            XName xnProperty = dataServicesNamespace + property.Name;
                            XElement xeEncryptThisProperty = xePayload.Descendants(xnProperty).First();
                            if (xeEncryptThisProperty == null)
                            {
                                //Couldn't find the value in the XML
                                continue;
                            }

                            //Encrypt the property 
                            string propertyValue = xeEncryptThisProperty.Value;
                            if (!string.IsNullOrEmpty(propertyValue))
                            {
                                XAttribute attr = xeEncryptThisProperty.Attributes(dataTypeName).FirstOrDefault();
                                string propertyType = (attr == null ?"Edm.String" : attr.Value);
                                
                                if (propertyType.Equals("Edm.String", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    string cryptedString = TableCrypto.EncryptStringAndBase64(this.EncryptionVersion.Value, propertyValue);
                                    xeEncryptThisProperty.Value = cryptedString;
                                }
                                else if (propertyType.Equals("Edm.Binary", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    byte[] plainBytes = Convert.FromBase64String(propertyValue);
                                    byte[] cryptedBytes = TableCrypto.Encrypt(this.EncryptionVersion.Value, plainBytes);
                                    xeEncryptThisProperty.Value = Convert.ToBase64String(cryptedBytes);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
