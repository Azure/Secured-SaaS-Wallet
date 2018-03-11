using Blockchain;
using Communication;
using Communication.AzureQueueDependencies;
using Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static Cryptography.KeyVaultCryptoActions;

namespace WalletApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            Configuration = builder.Build();
        }
        

        public IConfiguration Configuration { get; }
        public KeyVault KV;
        public AzureQueue azureQueue;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddSingleton((serviceProvider) =>
            {
                var sqlDb = new SqlConnector(Configuration["SqlUserID"],
                Configuration["SqlPassword"],
                Configuration["SqlInitialCatalog"],
                Configuration["SqlDataSource"],
                Configuration["applicationId"],
                Configuration["applicationSecret"]);
                sqlDb.Initialize().Wait();

                return new EthereumAccount(sqlDb, Configuration["EthereumNodeUrl"]);
            });

            services.AddSingleton<IQueue, AzureQueue>((serviceProvider) =>
            {
                const string queueName = "transactions";
                KV = new KeyVault(Configuration["AzureKeyVaultUri"],
                    Configuration["applicationId"], Configuration["applicationSecret"]);
                var encryptionKeyName = Configuration["EncryptionKeyName"];
                var decryptionKeyName = Configuration["DecryptionKeyName"];
                var signKeyName = Configuration["SignKeyName"];
                var verifyKeyName = Configuration["VerifyKeyName"];

                var encryptionCertPassword = Configuration["EncryptionCertPassword"];
                var decryptionCertPassword = Configuration["DecryptionCertPassword"];
                var signCertPassword = Configuration["SignCertPassword"];
                var verifyCertPassword = Configuration["VerifyCertPassword"];

                var secretsMgmnt = new KeyVaultCryptoActions(
                    new CertificateInfo(encryptionKeyName, encryptionCertPassword),
                    new CertificateInfo(decryptionKeyName, decryptionCertPassword),
                    new CertificateInfo(signKeyName, signCertPassword),
                    new CertificateInfo(verifyKeyName, verifyCertPassword),
                    KV,
                    KV);
                secretsMgmnt.InitializeAsync().Wait();
                var queueClient = new CloudQueueClientWrapper(Configuration["AzureStorageConnectionString"]);
                azureQueue = new AzureQueue(queueName, queueClient, secretsMgmnt, true);
                azureQueue.InitializeAsync().Wait();

                return azureQueue;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
