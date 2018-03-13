using System;
using System.Configuration;
using System.Threading;
using Wallet.Blockchain;
using Wallet.Communication;
using Wallet.Communication.AzureQueueDependencies;
using Wallet.Cryptography;

namespace CoinsSender
{
    /// <summary>
    ///  A sample app that checks balance and while > some value
    ///  keep asking the transferee to create more transactions (Sends money)
    /// </summary>
    class Program
    {
        #region private members

        private const string c_ReciverId = "reciverAccount";
        private const string c_senderId = "senderAccount";

        #endregion

        static void Main(string[] args)
        {
            var sqlDb = new SqlConnector(ConfigurationManager.AppSettings["SqlUserID"],
                ConfigurationManager.AppSettings["SqlPassword"],
                ConfigurationManager.AppSettings["SqlInitialCatalog"],
                ConfigurationManager.AppSettings["SqlDataSource"]);
            sqlDb.Initialize().Wait();

            var ethereumAccount = new EthereumAccount(sqlDb, ConfigurationManager.AppSettings["EthereumNodeUrl"]);

            // If Ethereum node url is empty, the program will run with local Ethereum Test RPC
            // Otherwise, it will run with the real Ethereum TestNet.
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["EthereumNodeUrl"]))
            {
                Console.WriteLine("Running with Docker TestRpc");
                EthereumTestRpcDemo(ethereumAccount);
            }
            else
            {
                Console.WriteLine("Running with Ethereum Testnet");
                EthereumTestnetDemo(ethereumAccount);
            }
        }

        private static void EthereumTestRpcDemo(EthereumAccount ethereumAccount)
        {
            var senderPrivateKey = "0x4faec59e004fd62384813d760e55d6df65537b4ccf62f268253ad7d4243a7193";
            var reciverPrivateKey = "0x03fd5782c37523be6598ca0e5d091756635d144e42d518bb5f8db11cf931b447";

            Console.WriteLine($"Please run the docker image with the following command:{Environment.NewLine}" +
                              "docker run -d -p 8545:8545 trufflesuite/ganache-cli:latest " +
                              $"--account=\"{senderPrivateKey}, 300000000000000000000\"" +
                              $" --account=\"{reciverPrivateKey}, 0\"");

            // Check if Account already stored in KeyStore
            try
            {
                var senderAccount = ethereumAccount.GetPublicAddressAsync(c_senderId).Result;
                var reciverAccount = ethereumAccount.GetPublicAddressAsync(c_ReciverId).Result;
            }
            catch (Exception)
            {
                // TODO: Add Check for key not found exception 
                ethereumAccount.CreateAccountAsync(c_senderId, senderPrivateKey).Wait();
                ethereumAccount.CreateAccountAsync(c_ReciverId, reciverPrivateKey).Wait();
            }
            finally
            {
                SendCoins(ethereumAccount);
            }
        }

        private static void EthereumTestnetDemo(EthereumAccount ethereumAccount)
        {
            try
            {
                var senderAccount = ethereumAccount.GetPublicAddressAsync(c_senderId).Result;
                var reciverAccount = ethereumAccount.GetPublicAddressAsync(c_ReciverId).Result;
            }
            catch (Exception)
            {
                // TODO: Add Check for key not found exception 
                ethereumAccount.CreateAccountAsync(c_senderId).Wait();
                ethereumAccount.CreateAccountAsync(c_ReciverId).Wait();

                var senderPublicAddress = ethereumAccount.GetPublicAddressAsync(c_senderId);
                Console.WriteLine("Accounts were created. " +
                                  $"To continue the demo please send ether to address {senderPublicAddress}{Environment.NewLine}" +
                                  "You can send ether for: https://www.rinkeby.io/#faucet");
            }

            SendCoins(ethereumAccount);
        }

        private static void SendCoins(EthereumAccount ethereumAccount)
        {
            Console.WriteLine("Sender - Happy to transfer my crypto coins!");

            // Init
            var senderAddress = ethereumAccount.GetPublicAddressAsync(c_senderId).Result;
            var reciverAddress = ethereumAccount.GetPublicAddressAsync(c_ReciverId).Result;
            var balance = ethereumAccount.GetCurrentBalance(senderAddress).Result;
            PrintCurrentBalance(senderAddress, balance);

            var encryptionKeyName = ConfigurationManager.AppSettings["EncryptionKeyName"];
            var decryptionKeyName = ConfigurationManager.AppSettings["DecryptionKeyName"];
            var signKeyName = ConfigurationManager.AppSettings["SignKeyName"];
            var verifyKeyName = ConfigurationManager.AppSettings["VerifyKeyName"];

            var kv = new KeyVault(ConfigurationManager.AppSettings["AzureKeyVaultUri"],
                ConfigurationManager.AppSettings["applicationId"], ConfigurationManager.AppSettings["applicationSecret"]);
            var secretsMgmnt = new KeyVaultCryptoActions(encryptionKeyName, decryptionKeyName, signKeyName, verifyKeyName, kv, kv);
            secretsMgmnt.Initialize().Wait();
            //var securedComm = new RabbitMQBusImpl(ConfigurationManager.AppSettings["rabbitMqUri"], secretsMgmnt, true, "securedCommExchange");

            var queueClient = new CloudQueueClientWrapper(ConfigurationManager.AppSettings["AzureStorageConnectionString"]);
            var securedComm = new AzureQueue("transactions", queueClient, secretsMgmnt, true);
            securedComm.Initialize().Wait();

            // While there are sufficient funds, transfer some...
            while (balance >= 0)
            {
                var amountToSend = 0.001;
                // Message structure: {amountToSend};{senderName};{reciverAddress}
                var message = $"{amountToSend};{c_senderId};{reciverAddress}";
                securedComm.EnqueueAsync(Utils.ToByteArray(message)).Wait();

                // Sleep 1 minute
                Thread.Sleep(60000);

                var newBalance = ethereumAccount.GetCurrentBalance(senderAddress).Result;
                PrintCurrentBalance(senderAddress, newBalance);

                // Wait for mining.. 
                while (newBalance.Equals(balance))
                {
                    newBalance = ethereumAccount.GetCurrentBalance(senderAddress).Result;
                }

                balance = newBalance;
            }
        }

        public static void PrintCurrentBalance(string address, decimal balance)
        {
            Console.WriteLine($"Account {address} balance: {balance}");
        }
    }
}
