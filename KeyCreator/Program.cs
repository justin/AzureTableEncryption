using EncryptDecrypt;
using System;
using Microsoft.WindowsAzure.Storage;

namespace KeyCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            //Before running this you need to determine a certificate thumbprint
            //You can access this the certificate manager snapin (certmgr.msc)
            //You can use create a new, self-signed certificate for this purpose if you wish, using the following command line
            //makecert -sr LocalMachine -ss My -n "CN=AzureTableEncryption v1" -pe -sky exchange -len 2048
            //
            //Note, you may need to set permissions for the private key of the certificate. Determine what user runs your w3wp process (probably Network Service)
            //In the Certificate Manager Snapin (certmgr.msc) find your cert, right click -> Manage Private Keys
            //Add the correct user and grant "read" permissions.

            if (args.Length < 3)
            {
                Console.Out.WriteLine("Usage: KeyCreator <newEncryptionVersion> <certificateThumbprint> <StorageConnectString>");
                return;
            }

            int encryptionVersion;
            CloudStorageAccount acct;
            string certThumbprint = args[1];

            if (!int.TryParse(args[0], out encryptionVersion))
            {
                Console.Out.WriteLine("Could not parse \"{0}\" as an encryption version number", args[0]);
                return;
            }

            if (args[2].Equals("UseDevelopmentStorage=true", StringComparison.InvariantCultureIgnoreCase))
            {
                //Working around a bug in October 2012 release of storage client
                acct = CloudStorageAccount.DevelopmentStorageAccount;
            }
            else if (!CloudStorageAccount.TryParse(args[2], out acct))
            {
                Console.Out.WriteLine("Could not parse \"{0}\" as an azure storage connection string", args[2]);
                return;
            }

            AzureTableKeyGenerator keyGen = new AzureTableKeyGenerator(certThumbprint);
            keyGen.CreateNewKey(acct, encryptionVersion);
        }
    }
}
