using System;
using System.Threading.Tasks;

namespace Communication
{
    /// <summary>
    /// Interface for a queue based communication pipeline
    /// </summary>
    public interface IQueue
    {
        /// <summary>
        /// Enqueue a message to the queue
        /// </summary>
        /// <param name="msg">Message.</param>
        Task EnqueueAsync(byte[] msg);

        /// <summary>
        /// Creates a listener on a queue.
        /// </summary>
        /// <returns>The listener's identifier</returns>
        /// <param name="callbackOnSuccess">a callback to execute once a message arrives and verification succeeded</param>
        /// <param name="callbackOnFailure">a callback to execute if the verification failed</param>
        Task<string> DequeueAsync(Action<byte[]> callbackOnSuccess, Action<Message> callbackOnFailure);

        /// <summary>
        /// Creates a listener on a queue.
        /// </summary>
        /// <returns>The listener's identifier</returns>
        /// <param name="callbackOnSuccess">a callback to execute once a message arrives</param>
        /// <param name="callbackOnFailure">a callback to execute once the verification failed</param>
        /// <param name="waitTime">The time to wait between messages</param>
        Task DequeueAsync(Action<byte[]> callbackOnSuccess, Action<Message> callbackOnFailure, TimeSpan waitTime);
    }
}