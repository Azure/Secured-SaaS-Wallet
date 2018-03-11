using System;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Communication.AzureQueueDependencies
{
    public interface ICloudQueueClientWrapper
    {
        ICloudQueueWrapper GetQueueReference(string queueName);
    }

    public class CloudQueueClientWrapper : ICloudQueueClientWrapper
    {
        private readonly Lazy<CloudQueueClient> _cloudQueueClient;

        public CloudQueueClientWrapper(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Connection string doesn't contain value");
            }

            _cloudQueueClient = new Lazy<CloudQueueClient>(() =>
            {
                // First connect to our Azure storage.
                var storageAccount = CloudStorageAccount.Parse(connectionString);

                // Create the queue client.
                return storageAccount.CreateCloudQueueClient();
            });
        }

        public ICloudQueueWrapper GetQueueReference(string queueName)
        {
            if (string.IsNullOrEmpty(queueName))
            {
                throw new ArgumentException("queueName doesn't contain value");
            }

            var cloudQueue = _cloudQueueClient.Value.GetQueueReference(queueName);
            return new CloudQueueWrapper(cloudQueue);
        }
    }
}