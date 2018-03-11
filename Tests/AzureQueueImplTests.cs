using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using UnitTests.Mocks;
using Xunit;
using Communication;
using Cryptography;
using static Cryptography.KeyVaultCryptoActions;

namespace UnitTests
{
    public class AzureQueueImplTests
    {
        [Fact]
        public async Task Test_Exception_Is_Thrown_When_Initialize_Not_CalledAsync()
        {
            var queueMock = new CloudQueueClientWrapperMock();
            var azureQueue = new AzureQueue("queueName", queueMock, new CryptoActionsManagerMock(), true);

            try
            {
                await azureQueue.EnqueueAsync(Communication.Utils.ToByteArray("some message"));
            }
            catch (SecureCommunicationException ex)
            {
                Assert.Equal("Object was not initialized", ex.Message);
            }
        }

        [Fact]
        public async Task Test_Enqueue_Message_Happy_flow()
        {
            // Init
            var queueMock = new CloudQueueClientWrapperMock();
            var keyVaultMock = new DatabaseMock("url");
            var encryptionManager = new KeyVaultCryptoActions(
                new CertificateInfo("emc", string.Empty),
                new CertificateInfo("emc", string.Empty),
                new CertificateInfo("emc", string.Empty),
                new CertificateInfo("emc", string.Empty),
                keyVaultMock,
                keyVaultMock);
            await encryptionManager.InitializeAsync();

            var queueName = "queueName";
            var azureQueue = new AzureQueue(queueName, queueMock, encryptionManager, true);
            await azureQueue.InitializeAsync();

            // Enqueue message
            var msg = "new message";
            await azureQueue.EnqueueAsync(Communication.Utils.ToByteArray(msg));

            var queueRefernce = queueMock.GetQueueReference(queueName);

            var result = await queueRefernce.GetMessageAsync(TimeSpan.FromSeconds(10),
                new QueueRequestOptions(), new OperationContext());

            var encryptedMessage = Communication.Utils.FromByteArray<Message>(result.AsBytes);
            // String is encrypted, check it value
            Assert.Equal(256, encryptedMessage.Data.Length);
        }

        [Fact]
        public async Task Test_AzureImpl_Enqueue_Dequeue()
        {
            // Init
            var queueMock = new CloudQueueClientWrapperMock();
            var keyVaultMock = new DatabaseMock("url");
            var encryptionManager = new KeyVaultCryptoActions(
                new CertificateInfo("emc", string.Empty),
                new CertificateInfo("emc", string.Empty),
                new CertificateInfo("emc", string.Empty),
                new CertificateInfo("emc", string.Empty),
                keyVaultMock,
                keyVaultMock);
            await encryptionManager.InitializeAsync();

            var queueName = "queueName";
            var azureQueue = new AzureQueue(queueName, queueMock, encryptionManager, true);
            await azureQueue.InitializeAsync();

            // Enqueue Message
            var msg = "new message";
            await azureQueue.EnqueueAsync(Communication.Utils.ToByteArray(msg));

            var task = azureQueue.DequeueAsync(decrypted =>
                {
                    // Verify that the decrypted message equals to the original
                    Assert.Equal(msg, Communication.Utils.FromByteArray<string>(decrypted));
                }, (message) => { Console.WriteLine("Verification failure, doing nothing"); },
                TimeSpan.FromMilliseconds(1));

            Thread.Sleep(10000);
            azureQueue.CancelListeningOnQueue();

            await task;
        }
    }
}