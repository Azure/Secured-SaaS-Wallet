using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Cryptography
{
    /// <summary>
    /// Manages the crypto operations which can be applied on a message using the loaded certificates.
    /// </summary>
    public class CertificatesCryptoActions : ICryptoActions
    {
        #region private members

        private readonly X509Certificate2 m_encryptionCert;
        private readonly X509Certificate2 m_decryptionCert;
        private readonly X509Certificate2 m_signCert;
        private readonly X509Certificate2 m_verifyCert;

        #endregion

        public CertificatesCryptoActions(X509Certificate2 encryptionCert, X509Certificate2 decryptionCert,
            X509Certificate2 signCert, X509Certificate2 verifyCert)
        {
            m_signCert = signCert;
            m_verifyCert = verifyCert;
            m_encryptionCert = encryptionCert;
            m_decryptionCert = decryptionCert;
        }

        /// <summary>
        /// Decrypt the specified encryptedData.
        /// </summary>
        /// <param name="encryptedData">Encrypted data.</param>
        /// <returns>The decrypted data</returns>
        public byte[] Decrypt(byte[] encryptedData)
        {
            if (encryptedData == null) throw new ArgumentNullException(nameof(encryptedData));

            try
            {
                // GetRSAPrivateKey returns an object with an independent lifetime, so it should be
                // handled via a using statement.
                using (var rsa = m_decryptionCert.GetRSAPrivateKey())
                {
                    return rsa.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA1);
                }
            }
            catch (CryptographicException exc)
            {
                Console.WriteLine($"Exception was thrown while decrypting the data: {exc}");
                throw;
            }
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Encrypt the specified data.
        /// </summary>
        /// <param name="data">Data to be encrypted.</param>
        /// <returns>Encrypted data</returns>
        public byte[] Encrypt(byte[] data)
        {
            try
            {
                // GetRSAPublicKey returns an object with an independent lifetime, so it should be
                // handled via a using statement.
                using (var rsa = m_encryptionCert.GetRSAPublicKey())
                {
                    // OAEP allows for multiple hashing algorithms, what was formerly just "OAEP" is
                    // now OAEP-SHA1.
                    return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA1);
                }
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine($"Exception was thrown while encrypting the data: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Sign the specified data.
        /// </summary>
        /// <param name="data">The data to be signed</param>
        /// <returns>The signature</returns>
        public byte[] Sign(byte[] data)
        {
            // Verify input
            if (data == null) throw new ArgumentNullException(nameof(data));

            using (var rsa = m_signCert.GetRSAPrivateKey())
            {
                return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
        }

        /// <summary>
        /// Verify the specified signature and data.
        /// </summary>
        /// <param name="signature">The signature for verify</param>
        /// <param name="data">The data which match the signature</param>
        /// <returns>Boolean indicates whether the verification succeeded</returns>
        public bool Verify(byte[] data, byte[] signature)
        {
            // Verify inputs
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (signature == null) throw new ArgumentNullException(nameof(signature));

            // Verify data
            using (var rsa = m_verifyCert.GetRSAPublicKey())
            {
                return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
        }
    }
}