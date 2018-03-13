using System;
using System.Runtime.Serialization;

namespace Wallet.Cryptography
{
    public class CryptoException : Exception
    {
        public CryptoException()
        {
        }

        public CryptoException(string message) : base(message)
        {
        }

        public CryptoException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public CryptoException(SerializationInfo serializationInfo, StreamingContext streamingContext) : 
            base(serializationInfo, streamingContext)
        {
        }
    }

    public class DecryptionException : CryptoException
    {
        public DecryptionException()
        {
        }

        public DecryptionException(string message) : base(message)
        {
        }

        public DecryptionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public DecryptionException(SerializationInfo serializationInfo, StreamingContext streamingContext) :
            base(serializationInfo, streamingContext)
        {
        }
    }

    public class EncryptionException : CryptoException
    {
        public EncryptionException()
        {
        }

        public EncryptionException(string message) : base(message)
        {
        }

        public EncryptionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public EncryptionException(SerializationInfo serializationInfo, StreamingContext streamingContext) :
            base(serializationInfo, streamingContext)
        {
        }
    }
}