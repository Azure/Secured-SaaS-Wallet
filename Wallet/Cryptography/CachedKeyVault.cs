using System;
using System.Threading.Tasks;
using Wallet.Communication;
using StackExchange.Redis;
using Microsoft.Azure.KeyVault.Models;
using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Wallet.Cryptography
{
    /// <summary>
    /// KV client with redis cache layer. 
    /// </summary>
    public class CachedKeyVault : ISecretsStore
    {
        #region private members

        private bool m_isInitialized;
        private IDatabase m_db;
        private ConnectionMultiplexer m_redis;
        private string m_connectionString;
        private ICryptoActions m_cryptoActions;
        private ISecretsStore m_keyVault;
        private TelemetryClient m_telemetryClient;

        #endregion
        
        public CachedKeyVault(string connectionString, ISecretsStore keyVault, ICryptoActions cryptoActions)
        {
            m_isInitialized = false;
            m_connectionString = connectionString;

            m_keyVault = keyVault ?? throw new ArgumentNullException(nameof(keyVault)); ;
            m_cryptoActions = cryptoActions ?? throw new ArgumentNullException(nameof(cryptoActions));
            m_telemetryClient = new TelemetryClient();
    }

        public void Initialize()
        {
            if (m_isInitialized)
            {
                throw new SecureCommunicationException("Object was already initialized");
            }

            ConfigurationOptions options = ConfigurationOptions.Parse(m_connectionString);
            m_redis = ConnectionMultiplexer.Connect(options);
            m_db = m_redis.GetDatabase();

            m_isInitialized = true;
        }

        /// <summary>
        /// Stores the secret in the SQL data base.
        /// </summary>
        /// <param name="identifier">The secret id</param>
        /// <param name="privateKey">The secret private key</param>
        public async Task SetSecretAsync(string identifier, string privateKey)
        {
            ThrowIfNotInitialized();

            // The encryptedSecret will be saved ENCRYPTED.
            var encryptedSecret = Wallet.Communication.Utils.FromByteArray<string>(m_cryptoActions.Encrypt(Wallet.Communication.Utils.ToByteArray(privateKey)));
            
            // stored UNEncrypted in keyvault, as keyvault is already safe
            // If a previous encryptedSecret exists, it will be overwritten
            var kvTask = m_keyVault.SetSecretAsync(identifier, privateKey);

            // But ENCRYPTED in redis
            // If a previous encryptedSecret exists, it will be overwritten
            var redisTask = m_db.StringSetAsync(identifier, encryptedSecret);

            await Task.WhenAll(new Task[] { kvTask, redisTask });
        }

        /// <summary>
        /// Gets the secret from the SQL database
        /// </summary>
        /// <param name="identifier">The secret identifier</param>
        /// <returns>The secret from the data base</returns>
        public async Task<string> GetSecretAsync(string identifier)
        {

            ThrowIfNotInitialized();
            var sw = Stopwatch.StartNew();

            var rawValue = await m_db.StringGetAsync(identifier);

            // key not present in redis
            if (rawValue.IsNullOrEmpty)
            {
                // Get from KV (returns in unencrypted format)
                var secret = "";
                try
                {
                    secret = await m_keyVault.GetSecretAsync(identifier);
                }
                catch (KeyVaultErrorException exc)
                {
                    m_telemetryClient.TrackException(exc);
                    throw new SecureCommunicationException($"key: '{identifier}' was not found in KV", exc);
                }catch(Exception exc)
                {
                    var s = exc.Message;
                }

                // Store in Redis (in Encrypted way)
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                m_db.StringSetAsync(
                    identifier, 
                    m_cryptoActions.Encrypt(Wallet.Communication.Utils.ToByteArray(secret)));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                sw.Stop();
                m_telemetryClient.TrackMetric(new MetricTelemetry("KV-Get-4", sw.ElapsedMilliseconds));
                // track
                return secret;
            }

            sw.Stop();
            m_telemetryClient.TrackMetric(new MetricTelemetry("Redisd-Get-4", sw.ElapsedMilliseconds));
            return Wallet.Communication.Utils.FromByteArray<string>(m_cryptoActions.Decrypt(rawValue));
        }

        #region privateMethods

        private void ThrowIfNotInitialized()
        {
            if (!m_isInitialized)
            {
                throw new SecureCommunicationException("Object was not initialized");
            }
        }
        #endregion
    }
}
