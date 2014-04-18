# Azure Table Encryption via Attribute

## Project Description

SSL isn't enough when storing data in the cloud. You need to protect data-at-rest from anyone who has access to your store. In addition your SSL data may be vulnerable to a man-in-the-middle technology or IT shops that inspect and log the SSL contents. Bluecoat is one example that I've personally worked with and it operates just like Fiddler, but the threat exists. 

With this project, you can use a single attribute to transparently [Encrypt][encrypt] and Decrypt data when saving or reading data from Azure Table storage. You can then use your uploaded SSL key (or any other [x509][x509] certificate) to maintain security of your data in the cloud.

For more information see the [FAQ][faq]

## The Solution
We can leverage the security and trust of the Azure certificate store, and use that as a cryptographic foundation for the rest of our encryption. The slowness of the cryptographic functions will be isolated to the encryption and decryption of the [symmetric key][symmetric]. 

## How it works

The approach this sample application takes is to combine the security of the [x509][x509] certificate and speed of an AES [symmetric key][symmetric] to [encrypt][encrypt] the data. The process goes something like this:

## Writing an entity to Azure

A [x509][x509] certificate is loaded from the LocalMachine\User crypto store.
The `DataServiceContextEx_WritingEntity` event fires when the SaveChanges method is called.
If the event handler `DataServiceContextEx_WritingEntity` determines that the [Encrypt][encrypt] attribute was applied on a given property it will [encrypt][encrypt] the data
Then there will be an attempt to load the encrypted [symmetric key][symmetric] from Azure Table
The [symmetric key][symmetric] will be decrypted using the [x509][x509] certificate (slow and secure)
If there is no [symmetric key][symmetric] to be found, then a new one will be generated, then encrypted and saved
Note: The decrypted [symmetric key][symmetric] resides only in RAM, and this is used to quickly [encrypt][encrypt] the data saved to Azure table

## Reading an entity from Azure

A [x509][x509] certificate is loaded from the LocalMachine\User crypto store, if not already done.
The `DataServiceContextEx_ReadingEntity` event fires when the SaveChanges method is called.
If the event handler `DataServiceContextEx_ReadingEntity` determines that the [Encrypt][encrypt] attribute was applied on a given property it will decrypt the data
The decrypted [symmetric key][symmetric] resides only in RAM, and this is used to quickly decrypt the data saved to Azure table

## What's on the wire?, What's saved in the cloud?

If you use Fiddler, Netmon, or any tool that exposes Azure Studio contents you will find the table you created with encrypted contents for every property you tagged Encrypt. There will also be a second table that maintains a list of encrypted [symmetric keys][symmetric]. There is a many to 1 relationship between [symmetric keys][symmetric] and [x509][x509] certificates. In future versions, it will be possible to rotate up to 9999 [symmetric keys][symmetric] per certificate.

## TODO

* Multi-Threading Support - The best way to avoid concurrency issues is to initialize the context object once, and then store that reference in a static variable.

* Right now there is only support for one [symmetric key][symmetric] per certificate, however in the future you will be able to rotate keys, and select new ones.

* Seeding. There is no seeding of encrypted hashes. That means that the encrypted data is vulnerable to a statistical analysis. 

* Property encryption. I'm exploring ways to compress and [encrypt][encrypt] the property names. Each property label is redundantly stored in Azure Table Storage. Since most labels are A-Z I'm exploring ways to save more data per byte using this library: http://baseanythingconvert.codeplex.com/ Basically I intend to convert the "ASCII" label as an array of Unicode bytes.

[symmetric]: https://github.com/glassboardapp/AzureTableEncryption/wiki/The-Symmetric-Key
[faq]: https://github.com/glassboardapp/AzureTableEncryption/wiki/[FAQ][faq]
[encrypt]: https://github.com/glassboardapp/AzureTableEncryption/wiki/The-Encrypt-Attribute
[x509]: https://github.com/glassboardapp/AzureTableEncryption/wiki/The-x509-Certificate
