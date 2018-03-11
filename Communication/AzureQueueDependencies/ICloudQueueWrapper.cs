using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Communication.AzureQueueDependencies
{
    public interface ICloudQueueWrapper
    {
        Task AddMessageAsync(CloudQueueMessage message);
        Task CreateIfNotExistsAsync();

        Task<CloudQueueMessage> GetMessageAsync(TimeSpan? visibilityTimeout, QueueRequestOptions options,
            OperationContext operationContext);

        Task DeleteMessageAsync(CloudQueueMessage message);
    }


    public class CloudQueueWrapper : ICloudQueueWrapper
    {
        private readonly CloudQueue _cloudQueue;

        public CloudQueueWrapper(CloudQueue cloudQueue)
        {
            _cloudQueue = cloudQueue ?? throw new ArgumentNullException(nameof(cloudQueue));
        }

        public async Task CreateIfNotExistsAsync()
        {
            await _cloudQueue.CreateIfNotExistsAsync();
        }

        public async Task<CloudQueueMessage> GetMessageAsync(TimeSpan? visibilityTimeout, QueueRequestOptions options,
            OperationContext operationContext)
        {
            return await _cloudQueue.GetMessageAsync(visibilityTimeout, options, operationContext);
        }

        public async Task DeleteMessageAsync(CloudQueueMessage message)
        {
            await _cloudQueue.DeleteMessageAsync(message);
        }

        public async Task AddMessageAsync(CloudQueueMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            await _cloudQueue.AddMessageAsync(message);
        }
    }
}