using System.Threading.Tasks;
using Wallet.Blockchain;
using Xunit;

namespace UnitTests
{
    public class EthereumNodeWrapperTest
    {
        [Fact]
        public async void Sanity_Get_Balance()
        {
            var kvInfo = new DatabaseMock("http://dummyKvUri");
            var ethereumWallet = new EthereumAccount(kvInfo, "https://rinkeby.infura.io/fIF86MY6m3PHewhhJ0yE");
            var transactionHash = await ethereumWallet.GetCurrentBalance(TestConstants.publicKey);

            Assert.IsType<decimal>(transactionHash);
        }

        [Fact]
        public async void Test_SendTransaction()
        {
            var kvInfo = new DatabaseMock("http://dummyKvUri");
            var ethereumWallet = new EthereumAccount(kvInfo, "https://rinkeby.infura.io/fIF86MY6m3PHewhhJ0yE");
            var transactionHash = await 
                ethereumWallet.SignTransactionAsync("sender", TestConstants.publicKey, 100);
            var transactionResult = await ethereumWallet.SendRawTransactionAsync(transactionHash);

            Assert.StartsWith("0x", transactionResult);
            Assert.Equal(66, transactionResult.Length);
        }
    }
}
