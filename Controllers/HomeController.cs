using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UniShare.Models;

namespace UniShare.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Dashboard()
        {
            var role = HttpContext.Session.GetString("UserRole");
            ViewBag.UserRole = role;
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            return View();
        }

    }
}
