using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Communication.AzureQueueDependencies;
using Cryptography;

namespace Communication
{
    public class AzureQueue : BaseQueue, IQueue
    {
        #region private members

        private const int MessagePeekTimeInSeconds = 60;

        private ICloudQueueWrapper m_queue;
        private readonly ICloudQueueClientWrapper m_queueClient;
        private bool m_isActive;
        private bool m_isInitialized;

        private readonly bool m_isEncrypted;
        private readonly string m_queueName;

        #endregion

        public AzureQueue(string queueName, ICloudQueueClientWrapper queueClient, ICryptoActions cryptoActions,
            bool isEncrypted) : base(cryptoActions)
        {
            m_queueClient = queueClient;
            m_isEncrypted = isEncrypted;
            m_isActive = false;
            m_queueName = queueName;
            m_isInitialized = false;
        }

        public async Task InitializeAsync()
        {
            m_queue = m_queueClient.GetQueueReference(m_queueName);
            await m_queue.CreateIfNotExistsAsync();

            m_isInitialized = true;
        }

        /// <summary>
        /// Enqueues a message, it will be automatically signed and if chosen (ctor) encrypted as well
        /// </summary>
        /// <param name="msg">Message.</param>
        public async Task EnqueueAsync(byte[] msg)
        {
            ThrowIfNotInitialized();

            var messageInBytes = CreateMessage(msg, m_cryptoActions, m_isEncrypted);

            try
            {
                await m_queue.AddMessageAsync(new CloudQueueMessage(messageInBytes));
            }
            catch (StorageException ex)
            {
                Console.WriteLine($"Exception was thrown when trying to push message to queue, exception: {ex}");
                throw;
            }
        }

        public Task<string> DequeueAsync(Action<byte[]> callbackOnSuccess, Action<Message> callbackOnFailure)
        {
            throw new SecureCommunicationException(
                "This method signature is not supported for the Azure BaseQueue implementation");
        }

        /// <summary>
        /// Dequeues a message. The signature will be verified, in case of a verification failure a failure callback will be called.
        /// The callback receives a single argument which is the decrypted and verified message
        /// </summary>
        /// <param name="callbackOnSuccess">Callback when message is verified</param>
        /// <param name="callbackOnFailure">Callback when verification failed</param>
        /// <param name="waitTime">Time to wait between dequeues</param>
        public Task DequeueAsync(Action<byte[]> callbackOnSuccess, Action<Message> callbackOnFailure, TimeSpan waitTime)
        {
            ThrowIfNotInitialized();

            m_isActive = true;
            CloudQueueMessage retrievedMessage = null;
            var dequeueTask = Task.Run(async () =>
            {
                while (m_isActive)
                {
                    retrievedMessage = await m_queue.GetMessageAsync(TimeSpan.FromSeconds(MessagePeekTimeInSeconds),
                        new QueueRequestOptions(), new OperationContext());
                    if (retrievedMessage != null)
                    {
                        ProccessMessage(callbackOnSuccess, callbackOnFailure, retrievedMessage.AsBytes);
                        await m_queue.DeleteMessageAsync(retrievedMessage);
                    }

                    Thread.Sleep((int) waitTime.TotalMilliseconds);
                }
            });

            return dequeueTask;
        }

        /// <summary>
        /// Stops the dequeuing process
        /// </summary>
        public void CancelListeningOnQueue()
        {
            ThrowIfNotInitialized();

            m_isActive = false;
        }

        #region privateMethods

        private void ThrowIfNotInitialized()
        {
            if (!m_isInitialized)
            {
                throw new SecureCommunicationException("Object was not initialized");
            }
        }

        #endregion
    }
}