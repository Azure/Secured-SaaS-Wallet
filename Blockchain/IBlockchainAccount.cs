using System.Numerics;
using System.Threading.Tasks;

namespace Blockchain
{
    /// <summary>
    /// Manage blockchain account and transactions.
    /// </summary>
    public interface IBlockchainAccount
    {
        /// <summary>
        /// Creates blockchain account if needed and store the private key in Azure KeyVault 
        /// </summary>
        /// <param name="identifier">key pair identifier.</param>
        /// <param name="privateKey">The given private key to store, if not supplied a new private key will be generated</param>
        /// <returns>The public address of the account</returns>
        Task<string> CreateAccountAsync(string identifier, string privateKey = "");

        /// <summary>
        /// Signs a blockchain transaction
        /// </summary>
        /// <param name="senderIdentifier">The sender identifier (Id, name, etc...)</param>
        /// <param name="recieverAddress">The receiver address</param>
        /// <param name="amountInWei">The amount to send</param>
        /// <returns>The signed transaction</returns>
        Task<string> SignTransactionAsync(string senderIdentifier, string recieverAddress, BigInteger amountInWei);

        /// <summary>
        /// Send the raw transaction to the public node. 
        /// </summary>
        /// <param name="signedTransaction">The transaction signed transaction</param>
        /// <returns>The transaction result</returns>
        Task<string> SendRawTransactionAsync(string signedTransaction);

        /// <summary>
        /// Returns the public address by the key vault identifier
        /// </summary>
        /// <param name="identifier">The user id</param>
        /// <returns>The user's public key</returns>
        Task<string> GetPublicAddressAsync(string identifier);

        /// <summary>
        /// Gets the balance of the provided account
        /// </summary>
        /// <param name="publicAddress">The public address of the account</param>
        /// <param name="identifier">The identifier of the account</param>
        /// <returns>Returns the balance in ether.</returns>
        Task<decimal> GetCurrentBalance(string publicAddress, string identifier);
    }
}