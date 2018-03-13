using System;
using Wallet.Cryptography;

namespace UnitTests
{
    public class CryptoActionsManagerMock : ICryptoActions
    {
        public byte[] Decrypt(byte[] encryptedData)
        {
            throw new NotImplementedException();
        }

        public byte[] Encrypt(byte[] data)
        {
            throw new NotImplementedException();
        }

        public byte[] Sign(byte[] data)
        {
            throw new NotImplementedException();
        }

        public bool Verify(byte[] data, byte[] signature)
        {
            throw new NotImplementedException();
        }
    }
}
