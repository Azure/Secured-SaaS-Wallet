using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wallet.Communication;
using Wallet.Communication.AzureQueueDependencies;
using Wallet.Cryptography;

namespace Performance
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            
        }

        public IConfiguration Configuration { get; }
        public KeyVault KV;
        public AzureQueue securedComm;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSingleton<KeyVault>((sp) =>
            {
                return new KeyVault(Configuration["AzureKeyVaultUri"],
                                    Configuration["applicationId"], Configuration["applicationSecret"]);
            });
            services.AddSingleton((sp) => {
                var encryptionKeyName = Configuration["EncryptionKeyName"];
                var decryptionKeyName = Configuration["DecryptionKeyName"];
                var signKeyName = Configuration["SignKeyName"];
                var verifyKeyName = Configuration["VerifyKeyName"];

                var ca = new KeyVaultCryptoActions(encryptionKeyName, decryptionKeyName, signKeyName, verifyKeyName, KV, KV);
                ca.Initialize().Wait();
                return ca;
            }
           
            );
            services.AddSingleton((sp) =>
            {
                var kv = new KeyVault(Configuration["AzureKeyVaultUri"],
                                    Configuration["applicationId"], Configuration["applicationSecret"]);
                var encryptionKeyName = Configuration["EncryptionKeyName"];
                var decryptionKeyName = Configuration["DecryptionKeyName"];
                var signKeyName = Configuration["SignKeyName"];
                var verifyKeyName = Configuration["VerifyKeyName"];

                var ca =  new KeyVaultCryptoActions(encryptionKeyName, decryptionKeyName, signKeyName, verifyKeyName, KV, KV);
                ca.Initialize().Wait();
                var red=  new CachedKeyVault(Configuration["CacheConnection"], kv, ca);
                red.Initialize();
                return red;
            });
            services.AddSingleton<IQueue, AzureQueue>((serviceProvider) =>
            {
                const string queueName = "somequeue";
                KV = new KeyVault(Configuration["AzureKeyVaultUri"],
                    Configuration["applicationId"], Configuration["applicationSecret"]);
                var encryptionKeyName = Configuration["EncryptionKeyName"];
                var decryptionKeyName = Configuration["DecryptionKeyName"];
                var signKeyName = Configuration["SignKeyName"];
                var verifyKeyName = Configuration["VerifyKeyName"];

                var secretsMgmnt = new KeyVaultCryptoActions(encryptionKeyName, decryptionKeyName, signKeyName, verifyKeyName, KV, KV);
                secretsMgmnt.Initialize().Wait();
                //var securedComm = new RabbitMQBusImpl(config["rabbitMqUri"], secretsMgmnt, true, "securedCommExchange");
                var queueClient = new CloudQueueClientWrapper(Configuration["AzureStorageConnectionString"]);
                securedComm = new AzureQueue(queueName, queueClient, secretsMgmnt, true);
                securedComm.Initialize().Wait();

                return securedComm;
            });

            services.AddSingleton((sp) => {
                 var sqlDb = 
                new SqlConnector(Configuration["SqlUserID"], Configuration["SqlPassword"], Configuration["SqlInitialCatalog"], Configuration["SqlDataSource"]);
                sqlDb.Initialize().Wait();
                return sqlDb;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
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
