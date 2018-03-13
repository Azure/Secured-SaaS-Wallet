using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Performance.Models;
using Wallet.Communication;

namespace Performance.Controllers
{
    public class HomeController : Controller
    {
        AzureQueue m_comm;

        public HomeController(IQueue _comm)
        {
            m_comm = (AzureQueue)_comm;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
