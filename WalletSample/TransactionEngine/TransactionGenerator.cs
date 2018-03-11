using System;
using System.Collections.Generic;
using System.Configuration;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blockchain;
using Communication;
using Communication.AzureQueueDependencies;
using Cryptography;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using static Cryptography.KeyVaultCryptoActions;

namespace TransactionGenerator
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class TransactionGenerator : StatelessService
    {
        public TransactionGenerator(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            var unitConverion = new Nethereum.Util.UnitConversion();

            var kv = new KeyVault(ConfigurationManager.AppSettings["AzureKeyVaultUri"],
                ConfigurationManager.AppSettings["applicationId"],
                ConfigurationManager.AppSettings["applicationSecret"]);

            var encryptionKeyName = ConfigurationManager.AppSettings["EncryptionKeyName"];
            var decryptionKeyName = ConfigurationManager.AppSettings["DecryptionKeyName"];
            var signKeyName = ConfigurationManager.AppSettings["SignKeyName"];
            var verifyKeyName = ConfigurationManager.AppSettings["VerifyKeyName"];

            var encryptionCertPassword = ConfigurationManager.AppSettings["EncryptionCertPassword"];
            var decryptionCertPassword = ConfigurationManager.AppSettings["DecryptionCertPassword"];
            var signCertPassword = ConfigurationManager.AppSettings["SignCertPassword"];
            var verifyCertPassword = ConfigurationManager.AppSettings["VerifyCertPassword"];

            var secretsMgmnt =
                new KeyVaultCryptoActions(
                    new CertificateInfo(encryptionKeyName, encryptionCertPassword),
                    new CertificateInfo(decryptionKeyName, decryptionCertPassword),
                    new CertificateInfo(signKeyName, signCertPassword),
                    new CertificateInfo(verifyKeyName, verifyCertPassword),
                    kv,
                    kv);
            secretsMgmnt.InitializeAsync().Wait();

            //var securedComm = new RabbitMQBusImpl(ConfigurationManager.AppSettings["rabbitMqUri"], secretsMgmnt, true, "securedCommExchange");
            var queueClient =
                new CloudQueueClientWrapper(ConfigurationManager.AppSettings["AzureStorageConnectionString"]);

            var securedCommForTransactions = new AzureQueue("transactions", queueClient, secretsMgmnt, true);
            var securedCommForNotifications = new AzureQueue("notifications", queueClient, secretsMgmnt, true);
            var taskInitTransactions = securedCommForTransactions.InitializeAsync();
            var taskInitNotifications = securedCommForNotifications.InitializeAsync();
            Task.WhenAll(taskInitTransactions, taskInitNotifications).Wait();

            var sqlDb = new SqlConnector(ConfigurationManager.AppSettings["SqlUserID"],
                ConfigurationManager.AppSettings["SqlPassword"],
                ConfigurationManager.AppSettings["SqlInitialCatalog"],
                ConfigurationManager.AppSettings["SqlDataSource"],
                ConfigurationManager.AppSettings["applicationId"],
                ConfigurationManager.AppSettings["applicationSecret"]);
            sqlDb.Initialize().Wait();
            var ethereumNodeWrapper = new EthereumAccount(sqlDb, ConfigurationManager.AppSettings["EthereumNodeUrl"]);

            // Listen on transactions requests, process them and notify the users when done
            securedCommForTransactions.DequeueAsync(
                msg =>
                {
                    Console.WriteLine("Got work!");

                    var data = Communication.Utils.FromByteArray<string>(msg);
                    var msgArray = data.Split(';');
                    var amount = unitConverion.ToWei(msgArray[0]);
                    var senderName = msgArray[1];
                    var reciverAddress = msgArray[2];

                    try
                    {
                        var transactionHash = ethereumNodeWrapper
                            .SignTransactionAsync(senderName, reciverAddress, amount).Result;

                        var transactionResult = ethereumNodeWrapper.SendRawTransactionAsync(transactionHash).Result;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        throw;
                    }

                    //TODO: Is this still needed?
                    // notify a user about his balance change
                    securedCommForNotifications.EnqueueAsync(Communication.Utils.ToByteArray(reciverAddress)).Wait();
                },
                (message) => { Console.WriteLine("Verification failure, doing nothing"); },
                TimeSpan.FromSeconds(3)).Wait();
        }
    }
}
