using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wallet.Communication;
using Wallet.Cryptography;

namespace Performance.Controllers
{
    public class SimplePerfController : Controller
    {
        AzureQueue m_comm;
        CachedKeyVault m_ckv;
        KeyVault m_kv;
        SqlConnector m_sql;
        public SimplePerfController(IQueue _comm, CachedKeyVault ckv, KeyVault kv, SqlConnector sql)
        {
            m_comm = (AzureQueue) _comm;
            m_ckv = ckv;
            m_kv = kv;
            m_sql = sql;
        }

        // GET: /<controller>/
        public IActionResult Index()
        {
            return View();
        }

        public string GenerateUsers()
        {
            var tsks = new Task[1000];
            for (var i = 0; i < 1000; i++)
            {
                tsks[i] = m_kv.SetSecretAsync(i.ToString(), "secretof" + i);
            }
            Task.WhenAll(tsks).Wait();
            return "Created 1000 users";
        }

        public string GenerateUsersSql()
        {
            var tsks = new Task[1000];
            for (var i = 0; i < 1000; i++)
            {
                tsks[i] = m_sql.SetSecretAsync(i.ToString(), "secretof" + i);
            }
            Task.WhenAll(tsks).Wait();
            return "Created 1000 users - sql";
        }

        public string Enqueue()
        {
            m_comm.EnqueueAsync(Utils.ToByteArray<string>("Hi Bye")).Wait();
            return "Enqueued";
        }

        public string StartDequeue()
        {
            m_comm.DequeueAsync((msg) =>
            {
                Console.WriteLine(msg);
            }, (msg) =>
            {
                Console.WriteLine("Failed processing message from queue");
            }, TimeSpan.FromSeconds(1));

            return "Started listening";
        }

        public string StopDequeue()
        {
            m_comm.CancelListeningOnQueue();

            return "Stopped";
        }

        public string GetAllSecretsRedis()
        {
            for (var i = 0; i < 4000; i++)
            {
                var index = i % 1000;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                 m_ckv.GetSecretAsync(index.ToString()).Wait();
                 /*if(i%1700 == 0)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(11));
                }*/
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }

            return "Got it";
        }

        public string GetAllSecretsSql()
        {
            for (var i = 0; i < 4000; i++)
            {
                var index = i % 1000;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                m_sql.GetSecretAsync(index.ToString()).Wait();
                /*if(i%1700 == 0)
               {
                   Thread.Sleep(TimeSpan.FromSeconds(11));
               }*/
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }

            return "Got it -SQL";
        }
    }


    
}