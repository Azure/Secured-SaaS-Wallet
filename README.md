# Core modules for crypto-currency virtual wallet
The project consists of the infrastructure Core modules needed for implementing a SaaS cryptocurrency virtual wallet. This project has the following modules:
### A secure communication library over a queue
For inter micro-services communication
### An Ethereum node client
For querying, signing and sending transactions and data over the public (and test) Ethereum network
### Secrets manager for the communication pipeline
For abstracting the needed secrets for the encryption/signing operations over the sent messages

This project also contains a [Sample](Sample) directory, to get you started.  

# Installation
1. `Contracts` contains all of the interfaces
2. `SecuredComm` contains the library implementation. consume it: clone the repository and add the dependency to the library.
(later we might release this as a nuget / package).
3. Usage examples:

## Ethereum node wrapper
```c#
// Create the instance
var ethereumNodeWrapper = new EthereumNodeWrapper(kv, ConfigurationManager.AppSettings["EthereumNodeUrl"]);

// Call methods
var result = await ethereumNodeWrapper.GetPublicAddressAsync("0x012345...");   
```

## Secrets Manager
```c#
// Create
var secretsMgmnt = new KeyVaultSecretManager(encryptionKeyName, decryptionKeyName, signKeyName, verifyKeyName, publicKv, privateKv);
// Initialize
await secretsMgmnt.Initialize();

// Call methods
secretsMgmnt.Encrypt(msgAsBytes);  
```
## Communication pipeline
```c#
// The following code enqueues a message to a queue named 'MyQueue'
// Create
var comm = new AzureQueueImpl("MyQueue", queueClient, secretsMgmnt, true);
// Init
await comm.Initialize();

// Enqueue messages
comm.EnqueueAsync("Some message meant for someone");

comm.DequeueAsync(msg =>
  {
    Console.WriteLine("Decrypted and Verified message is" : + msg);
  });
  
```

# Sample
## Installation instructions
### Prerequisites
1. An Azure subscription
2. Create a new Azure Active Directory application. This application will be used for authenticating against Azure: https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-integrating-applications
3. Get the service principal id and secret for the application from step 2

### Deploy resources via The setup script - This step is optional, you can create the resources manually as well
The setup script will create a new Azure Resource Group and a new Azure Storage and Azure KeyVault in the new resource group, finally it will upload a new certificate as a secret to the Key Vault.
Run the script:
1. Edit the parameters in the file [Sample/Deployment/oneclick.ps1](Sample/Deployment/oneclick.ps1), choose a region, a name for the resource groups, azure storage and keyvault
2. In a powershell console, go to [Sample/Deployment](Sample/Deployment)
3. run oneclick.ps1

### The sample apps
The sample contains 3 different processes. 
1. **Coins sender**: a user that sends Ethereum coins to the receiver account, this process writes a signed and encrypted message on the transaction queue with the transaction details (sender, receiver and amount).
2. **Transaction Engine**: Dequeues a pending transaction from the transactions queue, verifies the signature and decrypts the message.
Then it signs the transaction and send the signed transaction to the Ethereum node. Finally it writes a message to the notification queue, to notify that the transaction completed and the receiver's balance had changed.
3. **Coins receiver**: Listens on the notification queue and checks if there was a change in the receiver balance.

#### How to run Ethereum in the sample apps
- Option 1: Work with Ethereum testnet.
Create a token [here](https://infura.io/#how-to) and fill it in EthereumNodeUrl parameter in the App.config.
- Option 2: Work with local Ethereum node - TestRpc. 
The fastest way to run it is with Docker container, you should run it with the following command (it will automatically creats 2 accounts, one of them with 300 Ethereums):
```
docker run -d -p 8545:8545 trufflesuite/ganache-cli:latest --account="0x4faec59e004fd62384813d760e55d6df65537b4ccf62f268253ad7d4243a7193, 300000000000000000000" --account="0x03fd5782c37523be6598ca0e5d091756635d144e42d518bb5f8db11cf931b447, 0"
```
#### Running the sample apps
1. Edit App.Config for all 3 of the sample applications. Fill in the missing values. You should have all resources deployed at this stage, if you choose to skip the setup script, make sure you have all the needed resources
2. Compile and run all 3 projects in the following order: coins sender, transaction engine, coins receiver.
