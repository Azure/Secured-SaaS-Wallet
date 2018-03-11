using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Cryptography;

namespace UnitTests
{
    public class DatabaseMock : ISecretsStore
    {
        private string kvUri;

        public DatabaseMock(string kvUri)
        {
            this.kvUri = kvUri;
        }

        public string GetUrl()
        {
            return kvUri;
        }

        public Task<string> GetSecretAsync(string secretName)
        {
            Console.WriteLine("Starting get secret");
            if (secretName.Equals("sender"))
            {
                return Task.FromResult(TestConstants.privateKey);
            }

            var x = new X509Certificate2("../../../testCert.pfx", "abc123ABC", X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
            //var key = await GetKeyAsync(secretName);
            byte[] certBytes = x.Export(X509ContentType.Pkcs12);
            var certString = Convert.ToBase64String(certBytes);
            Console.WriteLine("finished get secret");
            return Task.FromResult(certString);
        }

        public Task SetSecretAsync(string secretName, string value)
        {
            throw new NotImplementedException();
        }

        public Task<bool> StoreKeyPairAsync(string identifier, string key)
        {
            throw new NotImplementedException();
        }
    }
}
