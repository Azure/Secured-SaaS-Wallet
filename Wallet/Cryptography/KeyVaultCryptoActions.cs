using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Org.BouncyCastle.Utilities.Encoders;

namespace Wallet.Cryptography
{
    /// <summary>
    /// An implementation of <see cref="ICryptoActions"/>, in this implementation the certificates
    /// are loaded from two given key vaults
    /// </summary>
    public class KeyVaultCryptoActions : ICryptoActions
    {
        #region private members

        private readonly ISecretsStore m_privateKeyVault;
        private readonly ISecretsStore m_publicKeyVault;

        private readonly string m_decryptionKeyName;
        private readonly string m_encryptionKeyName;
        private readonly string m_signKeyName;
        private readonly string m_verifyKeyName;

        private CertificatesCryptoActions _mCryptoActionsHelper;
        private bool m_isInit;

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SecuredCommunication.KeyVaultSecretManager"/> class.
        /// </summary>
        /// <param name="encryptionKeyName">Encryption key name.</param>
        /// <param name="decryptionKeyName">Decryption key name.</param>
        /// <param name="signKeyName">Sign key name.</param>
        /// <param name="verifyKeyName">Verify key name.</param>
        /// <param name="privateKv">A KV with private keys. Will be used for decryption and signing</param>
        /// <param name="publicKv">A KV just with public keys. Will be used for encryption and verifying</param>
        public KeyVaultCryptoActions(
            string encryptionKeyName,
            string decryptionKeyName, 
            string signKeyName, 
            string verifyKeyName, 
            ISecretsStore privateKv, 
            ISecretsStore publicKv)
        {
            // marked as false as we still need to initialize the EncryptionHelper later
            m_isInit = false;

            m_decryptionKeyName = decryptionKeyName;
            m_encryptionKeyName = encryptionKeyName;
            m_signKeyName = signKeyName;
            m_verifyKeyName = verifyKeyName;

            m_privateKeyVault = privateKv;
            m_publicKeyVault = publicKv;
        }

        /// <summary>
        /// Initialize the <see cref="CertificatesCryptoActions"/> object with all the certificates taken from the keyvaults
        /// </summary>
        public async Task Initialize() {

            // TODO: handle partial assignment of values
            var encryptSecretTask = m_publicKeyVault.GetSecretAsync(m_encryptionKeyName);
            var decryptSecretTask = m_privateKeyVault.GetSecretAsync(m_decryptionKeyName);
            var signSecretTask = m_publicKeyVault.GetSecretAsync(m_signKeyName);
            var verifySecretTask = m_publicKeyVault.GetSecretAsync(m_verifyKeyName);

            // wait on all of the tasks concurrently
            var tasks = new Task[] { encryptSecretTask, decryptSecretTask, signSecretTask, verifySecretTask };
            await Task.WhenAll(tasks);

            // when using 'Result' we know that the task is actually done already
            var encryptionCert = SecretToCertificate(encryptSecretTask.Result);
            var decryptionCert = SecretToCertificate(decryptSecretTask.Result);
            var signCert = SecretToCertificate(signSecretTask.Result);
            var verifyCert = SecretToCertificate(verifySecretTask.Result);

            // Now, we have an 'EncryptionHelper', which can help us encrypt, decrypt, sign and verify using
            // the pre-fetched certificates
            _mCryptoActionsHelper = new CertificatesCryptoActions(encryptionCert, decryptionCert, signCert, verifyCert);

            m_isInit = true;
        }


        public byte[] Decrypt(byte[] encryptedData)
        {
            VerifyInitialized();

            if (encryptedData == null)
            {
                throw new ArgumentNullException(nameof(encryptedData));
            }

            // Call Decrypt
            try
            {
                return _mCryptoActionsHelper.Decrypt(encryptedData);
            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public byte[] Encrypt(byte[] data)
        {
            VerifyInitialized();

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            try
            {
                return _mCryptoActionsHelper.Encrypt(data);
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public byte[] Sign(byte[] data)
        {
            VerifyInitialized();

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return _mCryptoActionsHelper.Sign(data);
        }

        public bool Verify(byte[] data, byte[] signature)
        {
            VerifyInitialized();

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (signature == null)
            {
                throw new ArgumentNullException(nameof(signature));
            }

            return _mCryptoActionsHelper.Verify(data, signature);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Takes a Base64 representation of a certificate and creates a new certificate
        /// object
        /// </summary>
        /// <returns>The certificate object</returns>
        /// <param name="secret">Base64 string representation of a certificate</param>
        private static X509Certificate2 SecretToCertificate(string secret)
        {
            if (string.IsNullOrEmpty(secret))
            {
                throw new ArgumentException("secret must be supplied");
            }

            return new X509Certificate2(Base64.Decode(secret));
        }

        /// <summary>
        /// Throw exception if not initialized
        /// </summary>
        private void VerifyInitialized()
        {
            if (!m_isInit)
            {
                throw new CryptoException("Initialize method needs to be called before accessing class methods");
            }
        }

        #endregion
    }
}