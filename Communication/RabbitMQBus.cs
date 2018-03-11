using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Cryptography;

namespace Communication
{
    // An implementation using the RabbitMQ service
    public class RabbitMQBus : BaseQueue, IQueue
    {
        #region private members

        private readonly bool m_isEncrypted;
        private readonly string m_exchangeName;

        private IModel m_channel;
        private EventingBasicConsumer m_consumer;
        private bool m_isInitialized;
        private readonly string m_rabitMqUri;
        private IBasicProperties m_queueProperties;
        private readonly string m_queueName;

        #endregion

        public RabbitMQBus(
            string rabitMqUri,
            ICryptoActions cryptoActions,
            bool isEncrypted,
            string exchangeName,
            string queueName) : base(cryptoActions)
        {
            // Sanity
            if (string.IsNullOrEmpty(rabitMqUri) ||
                string.IsNullOrEmpty(exchangeName) | string.IsNullOrEmpty(queueName))
            {
                throw new ArgumentException("RabbitMQ uri, exchange name and queue name must be supplied");
            }

            m_exchangeName = exchangeName;
            m_rabitMqUri = rabitMqUri;
            m_isEncrypted = isEncrypted;
            m_queueName = queueName;
        }

        public void Initialize()
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(m_rabitMqUri)
            };

            var conn = factory.CreateConnection();
            m_channel = conn.CreateModel();
            m_channel.ExchangeDeclare(m_exchangeName, ExchangeType.Direct);

            CreateQueue(m_queueName);

            m_queueProperties = m_channel.CreateBasicProperties();
            m_queueProperties.Persistent = true;
            m_channel.BasicQos(0, 1, false);

            m_isInitialized = true;
        }

        public Task<string> DequeueAsync(Action<byte[]> callbackOnSuccess, Action<Message> callbackOnFailure)
        {
            ThrowIfNotInitialized();

            if (callbackOnSuccess == null)
            {
                throw new ArgumentException("callback cannot be null");
            }

            m_consumer = new EventingBasicConsumer(m_channel);
            m_consumer.Received += (ch, ea) =>
            {
                // Ack to the queue that we got the message
                // TODO: handle messages that failed
                m_channel.BasicAck(ea.DeliveryTag, false);
                ProccessMessage(callbackOnSuccess, callbackOnFailure, ea.Body);
            };

            // return the consumer tag
            return Task.FromResult(m_channel.BasicConsume(m_queueName, false, m_consumer));
        }

        public Task DequeueAsync(Action<byte[]> callbackOnSuccess, Action<Message> callbackOnFailure, TimeSpan waitTime)
        {
            throw new SecureCommunicationException(
                "This method signature is not supported for the rabbitMQ implementation");
        }

        public Task EnqueueAsync(byte[] data)
        {
            ThrowIfNotInitialized();

            var msgAsBytes = CreateMessage(data, m_cryptoActions, m_isEncrypted);
            m_channel.BasicPublish(
                exchange: m_exchangeName,
                routingKey: m_queueName,
                mandatory: false,
                basicProperties: m_queueProperties,
                body: msgAsBytes);

            return Task.FromResult(0);
        }

        public void CancelListeningOnQueue(string consumerTag)
        {
            ThrowIfNotInitialized();

            if (string.IsNullOrEmpty(consumerTag))
            {
                throw new ArgumentException("consumer tag must be supplied");
            }

            m_channel.BasicCancel(consumerTag);
        }

        private void CreateQueue(string queueName)
        {
            ThrowIfNotInitialized();

            if (string.IsNullOrEmpty(queueName))
            {
                throw new ArgumentException("queue name must be supplied");
            }

            m_channel.QueueDeclare(queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            m_channel.QueueBind(queueName, m_exchangeName, queueName);
        }

        private void ThrowIfNotInitialized()
        {
            if (!m_isInitialized)
            {
                throw new SecureCommunicationException("Object was not initialized");
            }
        }
    }
}