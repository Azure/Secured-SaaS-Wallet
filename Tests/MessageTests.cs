using System;
using Wallet.Communication;
using Xunit;

namespace UnitTests
{
    public class MessageTests
    {
        [Fact]
        public void Sanity_VerifyMessageIsCreate()
        {
            var isEncrypted = true;
            var data = new byte[] {0, 1, 2};
            var signature = new byte[] {3, 4, 5};
            var msg = new Message(isEncrypted, data, signature);
        }

        [Fact]
        public void Sanity_ExceptionIsThrownWhenInputIsNull()
        {
            var isEncrypted = true;
            var data = new byte[] {0, 1, 2};
            var signature = new byte[] {3, 4, 5};

            try
            {
                var message = new Message(isEncrypted, null, signature);
            }
            catch (ArgumentNullException e)
            {
                Assert.Equal("Value cannot be null.\r\nParameter name: data", e.Message);
            }

            try
            {
                var msg = new Message(isEncrypted, data, null);
            }
            catch (ArgumentNullException e)
            {
                Assert.Equal("Value cannot be null.\r\nParameter name: signature", e.Message);
            }

        }
    }
}