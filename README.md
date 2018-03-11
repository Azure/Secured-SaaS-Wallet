# Core modules for crypto-currency virtual wallet [![Build Status](https://travis-ci.org/Azure/Secured-SaaS-Wallet.svg?branch=master)](https://travis-ci.org/Azure/Secured-SaaS-Wallet)
The project consists of the infrastructure Core modules needed for implementing a SaaS cryptocurrency virtual wallet. This project has the following modules:
### Secrets manager for the communication pipeline
For abstracting the needed secrets for the encryption/signing operations over the sent messages
### A secure communication library over a queue
For inter micro-services communication
### An Ethereum node client
For querying, signing and sending transactions and data over the public (and test) Ethereum network

This project also contains a [Sample](WalletSample) directory, to get you started.  

# Installation
The project contains three components:
1. `Blockchain` - Blockchain (Currently only Ethereum implementation) related functionality <br>
2. `Communication` - Communication pipeline between micro-services.<br>
3. `Cryptography` - Provides functionality for saving the users secrets (private keys) and for securing the micro-services communication pipeline <br>

To consume, clone the repository and add the projects as dependencies.

# Usage examples:

## Secrets Manager
```c#

// Create
var kv = new KeyVault(...);

var secretsMgmnt =
                new KeyVaultCryptoActions(
                    new CertificateInfo(encryptionKeyName, encryptionCertPassword),
                    new CertificateInfo(decryptionKeyName, decryptionCertPassword),
                    new CertificateInfo(signKeyName, signCertPassword),
                    new CertificateInfo(verifyKeyName, verifyCertPassword),
                    kv,
                    kv);

// Initialize
await secretsMgmnt.InitializeAsync();

// Call methods
var rawData = "Some text";
var encryptedData = secretsMgmnt.Encrypt(Communication.Utils.ToByteArray(rawData));
var originalData = secretsMgmnt.Decrypt(encryptedData);

```
## Communication pipeline
```c#
// The following code enqueues a message to a queue named 'MyQueue'
var secretsMgmnt = new KeyVaultCryptoActions(...);
secretsMgmnt.InitializeAsync().Wait();

var queueClient = new CloudQueueClientWrapper(ConfigurationManager.AppSettings["AzureStorageConnectionString"]);
// Create
var securedComm = new AzureQueue("MyQueue", queueClient, secretsMgmnt, true);
// Init
await securedComm.InitializeAsync();

// Enqueue messages
await securedComm.EnqueueAsync(Communication.Utils.ToByteArray("A message"));

 securedComm.DequeueAsync(
   msg =>
   {
      Console.WriteLine("Decrypted and Verified message is" : + msg);
   });
  
```

## Ethereum node client
```c#
// Create the instance of the Sql connector (which holds the users' private keys)
var sqlDb = new SqlConnector(...);
// Create the instance
var ethereumNodeClient = new EthereumAccount(sqlDb, ConfigurationManager.AppSettings["EthereumNodeUrl"]);

// Call methods
var result = await ethereumNodeClient.GetPublicAddressAsync("0x012345...");
```

## Sample
Sample wallet app that uses the provided libraries can be found [here](/WalletSample/README.md)


## Known Issues
* Getting 'access denied' when the script trys to set a new secret into KeyVault: wrong object id was entered. refer to the pre-requisites step and make sure you are using the correct object id.


## Contributing
See instructions [here](/CONTRIBUTING.md).</br>
By participating in this project, you agree to abide by the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct)
