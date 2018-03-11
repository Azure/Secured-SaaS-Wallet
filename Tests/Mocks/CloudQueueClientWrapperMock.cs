using System.Collections.Generic;
using Communication.AzureQueueDependencies;

namespace UnitTests.Mocks
{
    public class CloudQueueClientWrapperMock : ICloudQueueClientWrapper
    {
        private readonly Dictionary<string, ICloudQueueWrapper> _queuesDictionary = new Dictionary<string, ICloudQueueWrapper>();

        public ICloudQueueWrapper GetQueueReference(string queueName)
        {
            if (!_queuesDictionary.ContainsKey(queueName))
            {
                _queuesDictionary[queueName] = new CloudQueueWrapperMock(queueName);
            }

            return _queuesDictionary[queueName];
        }
    }
}
