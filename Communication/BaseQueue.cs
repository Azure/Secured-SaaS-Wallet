using Cryptography;
using System;
using System.Security.Cryptography;

namespace Communication
{
    /// <summary>
    /// Provides the common functionality for all queues
    /// </summary>
    public abstract class BaseQueue
    {
        protected readonly ICryptoActions m_cryptoActions;

        protected BaseQueue(ICryptoActions cryptoActions)
        {
            m_cryptoActions = cryptoActions ?? throw new ArgumentNullException(nameof(cryptoActions));
        }

        /// <summary>
        /// Deserialize, decrypts and verifies the message and the calls the appropriate callback.
        /// </summary>
        /// <param name="callbackOnSuccess">Callback on verify success</param>
        /// <param name="callbackOnFailure">Callback on verify failure</param>
        /// <param name="message">The message in bytes</param>
        protected void ProccessMessage(Action<byte[]> callbackOnSuccess, Action<Message> callbackOnFailure,
            byte[] message)
        {
            var deserializedMessage = DeserializedMessage(message);
            var data = deserializedMessage.Data;
            if (deserializedMessage.Encrypted)
            {
                data = m_cryptoActions.Decrypt(data);
            }

            if (m_cryptoActions.Verify(data, deserializedMessage.Signature))
            {
                callbackOnSuccess(data);
            }
            else
            {
                // Verification failed
                callbackOnFailure(deserializedMessage);
            }
        }

        /// <summary>
        /// Encrypts (if needed), signs and converts the message to byte array
        /// </summary>
        /// <param name="data">The data to send on the queue</param>
        /// <param name="cryptoActions">The encryption manager</param>
        /// <param name="isEncrypted">A flag that indicates whether the message needs to be encrypted</param>
        /// <returns>A byte array representing the message</returns>
        protected byte[] CreateMessage(byte[] data, ICryptoActions cryptoActions, bool isEncrypted)
        {
            if (cryptoActions == null)
            {
                throw new ArgumentNullException(nameof(cryptoActions));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            // Sign the message
            var signature = cryptoActions.Sign(data);

            if (isEncrypted)
            {
                try
                {
                    // Encrypt the message
                    data = cryptoActions.Encrypt(data);
                }
                catch (CryptographicException ex)
                {
                    throw new EncryptionException("Encryption failed", ex);
                }
            }
            else
            {
                Console.WriteLine("NOTICE: The enqueued message was NOT encrypted!");
            }

            // Convert the message to byte array
            return Utils.ToByteArray(new Message(isEncrypted, data, signature));
        }

        /// <summary>
        /// Deserialize the byte array to Message object
        /// </summary>
        /// <param name="messageInBytes">The message in bytes.</param>
        /// <returns>Returns whether the verification passed</returns>
        protected Message DeserializedMessage(byte[] messageInBytes)
        {
            return messageInBytes != null
                ? Utils.FromByteArray<Message>(messageInBytes)
                : throw new ArgumentNullException(nameof(messageInBytes));
        }
    }
}