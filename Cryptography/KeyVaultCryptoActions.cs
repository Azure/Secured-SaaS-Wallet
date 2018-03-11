using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Org.BouncyCastle.Utilities.Encoders;

namespace Cryptography
{
    /// <summary>
    /// An implementation of <see cref="ICryptoActions"/>, in this implementation the certificates
    /// are loaded from two given key vaults
    /// </summary>
    public class KeyVaultCryptoActions : ICryptoActions
    {
        public class CertificateInfo
        {
            public CertificateInfo(string name, string password)
            {
                Name = name;
                Password = password;
            }

            public string Name { get; }
            public string Password { get; }
        }

        #region private members

        private readonly ISecretsStore m_privateKeyVault;
        private readonly ISecretsStore m_publicKeyVault;

        private readonly CertificateInfo m_encryptionCertInfo;
        private readonly CertificateInfo m_decryptionCertInfo;
        private readonly CertificateInfo m_signCertInfo;
        private readonly CertificateInfo m_verifyCertInfo;

        private CertificatesCryptoActions _mCryptoActionsHelper;
        private bool m_isInit;

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SecuredCommunication.KeyVaultSecretManager"/> class.
        /// </summary>
        /// <param name="encryptionCertInfo">needed info for this certificate import process</param>
        /// <param name="decryptionCertInfo">needed info for this certificate import process</param>
        /// <param name="signCertInfo">needed info for this certificate import process</param>
        /// <param name="verifyCertInfo">needed info for this certificate import process</param>
        /// <param name="privateKv">A KV with private keys. Will be used for decryption and signing</param>
        /// <param name="publicKv">A KV just with public keys. Will be used for encryption and verifying</param>
        public KeyVaultCryptoActions(
            CertificateInfo encryptionCertInfo,
            CertificateInfo decryptionCertInfo,
            CertificateInfo signCertInfo,
            CertificateInfo verifyCertInfo,
            ISecretsStore privateKv,
            ISecretsStore publicKv)
        {
            // marked as false as we still need to initialize the EncryptionHelper later
            m_isInit = false;

            m_encryptionCertInfo = encryptionCertInfo;
            m_decryptionCertInfo = decryptionCertInfo;
            m_signCertInfo = signCertInfo;
            m_verifyCertInfo = verifyCertInfo;

            m_privateKeyVault = privateKv;
            m_publicKeyVault = publicKv;
        }

        /// <summary>
        /// InitializeAsync the <see cref="CertificatesCryptoActions"/> object with all the certificates taken from the keyvaults
        /// </summary>
        public async Task InitializeAsync()
        {
            // TODO: handle partial assignment of values
            var encryptSecretTask = m_publicKeyVault.GetSecretAsync(m_encryptionCertInfo.Name);
            var decryptSecretTask = m_privateKeyVault.GetSecretAsync(m_decryptionCertInfo.Name);
            var signSecretTask = m_publicKeyVault.GetSecretAsync(m_signCertInfo.Name);
            var verifySecretTask = m_publicKeyVault.GetSecretAsync(m_verifyCertInfo.Name);

            // wait on all of the tasks concurrently
            var tasks = new Task[] {encryptSecretTask, decryptSecretTask, signSecretTask, verifySecretTask};
            await Task.WhenAll(tasks);

            // when using 'Result' we know that the task is actually done already
            var encryptionCert = SecretToCertificate(encryptSecretTask.Result, m_encryptionCertInfo.Password);
            var decryptionCert = SecretToCertificate(decryptSecretTask.Result, m_decryptionCertInfo.Password);
            var signCert = SecretToCertificate(signSecretTask.Result, m_signCertInfo.Password);
            var verifyCert = SecretToCertificate(verifySecretTask.Result, m_verifyCertInfo.Password);

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
        /// <param name="certPassword">The password which protects the given certificate</param>
        private static X509Certificate2 SecretToCertificate(string secret, string certPassword)
        {
            if (string.IsNullOrEmpty(secret))
            {
                throw new ArgumentException("secret must be supplied");
            }

            return new X509Certificate2(Base64.Decode(secret), certPassword, X509KeyStorageFlags.PersistKeySet);
        }

        /// <summary>
        /// Throw exception if not initialized
        /// </summary>
        private void VerifyInitialized()
        {
            if (!m_isInit)
            {
                throw new CryptoException("InitializeAsync method needs to be called before accessing class methods");
            }
        }

        #endregion
    }
}