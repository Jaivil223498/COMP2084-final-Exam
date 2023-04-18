using AttaBoyGameStore.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AttaBoyGameStore.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Message"] = "Hello world";

            return View("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }








        public IActionResult AttaBoy()
        {
            return View("GameStore");
	    }
    }
}