using System;
using System.Runtime.Serialization;

namespace Communication
{
    /// <summary>
    /// This class will wrap all secure communication handled exceptions
    /// </summary>
    public class SecureCommunicationException : Exception
    {
        public SecureCommunicationException()
        {
        }

        public SecureCommunicationException(string message) : base(message)
        {
        }

        public SecureCommunicationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public SecureCommunicationException(SerializationInfo serializationInfo, StreamingContext streamingContext) :
            base(serializationInfo, streamingContext)
        {
        }
    }
}