using System.Threading.Tasks;

namespace Cryptography
{
    /// <summary>
    /// A wrapper that handles data base functionality
    /// </summary>
    public interface ISecretsStore
    {
        /// <summary>
        /// Gets the specified secret
        /// </summary>
        /// <returns>The secret</returns>
        /// <param name="secretName">Secret identifier</param>
        Task<string> GetSecretAsync(string secretName);

        /// <summary>
        /// Sets a secret in the database
        /// </summary>
        /// <returns>The secret.</returns>
        /// <param name="secretName">Secret identifier.</param>
        /// <param name="value">The value to be stored.</param>
        Task SetSecretAsync(string secretName, string value);
    }
}