using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests
{
    public class KeyVaultLoadTest
    {

        //[Fact]
        //public void KeyVaultLoadSetSecretTest()
        //{
        //    var kv = new KeyVault(ConfigurationManager.AppSettings["AzureKeyVaultUri"],
        //        ConfigurationManager.AppSettings["applicationId"], ConfigurationManager.AppSettings["applicationSecret"]);
        //    var tasks = new Task[2000];

        //    var count = 0;
        //    for (int i = 0; i < 7000000; i++)
        //    {
        //        try
        //        {
        //            count++;
        //            tasks[i % 2000] = kv.SetSecretAsync($"-{i}", "somesecret123");
        //            if (i % 2000 == 0 && i != 0)
        //            {
        //                Console.WriteLine(i);
        //                Task.WaitAll(tasks);
        //                Thread.Sleep(10000);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            var exCount = ex.GetInnerExceptions().ToList().Count;
        //            count = count - exCount;
        //            Console.WriteLine($"Exception in {i}, ex: {ex}");
        //        }

        //    }

        //    Console.WriteLine($"Successfully uploaded {count} secrets");
        //}
    }
}
