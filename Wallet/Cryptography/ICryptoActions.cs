namespace Wallet.Cryptography
{
    /// <summary>
    /// Provides the interface for all of the cryptographic actions which we
    /// need as part of the communication pipeline
    /// </summary>
    public interface ICryptoActions
    {
        /// <summary>
        /// Encrypt the specified data.
        /// </summary>
        /// <param name="data">Data to be encrypted.</param>
        /// <returns>Encrypted data</returns>
        byte[] Encrypt(byte[] data);

        /// <summary>
        /// Decrypt the specified encryptedData.
        /// </summary>
        /// <param name="encryptedData">Encrypted data.</param>
        /// <returns>The decrypted data</returns>
        byte[] Decrypt(byte[] encryptedData);

        /// <summary>
        /// Sign the specified data.
        /// </summary>
        /// <param name="data">The data to be signed</param>
        /// <returns>The signature</returns>
        byte[] Sign(byte[] data);

        /// <summary>
        /// Verify the specified signature and data.
        /// </summary>
        /// <param name="signature">The signature for verify</param>
        /// <param name="data">The data which match the signature</param>
        /// <returns>Boolean indicates whether the verification succeeded</returns>
        bool Verify(byte[] data, byte[] signature);
    }
}
