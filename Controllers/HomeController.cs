using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UniShare.Data;
using UniShare.Models;

namespace UniShare.Controllers
{
    public class HomeController : Controller
    {
        private readonly UniShareDbContext _context;
        public HomeController(UniShareDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Dashboard()
        {
            var role = HttpContext.Session.GetString("UserRole");
            ViewBag.UserRole = role;
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            if (role == "Driver")
            {
                int driverId = HttpContext.Session.GetInt32("UserId") ?? 0;
                ViewBag.TodayRides = _context.Rides
                    .Where(r => r.DriverId == driverId
                             && r.RideDate.Date == DateTime.Now.Date
                             && (r.RideStatus == "Upcoming" || r.RideStatus == "Active"))
                    .OrderBy(r => r.RideTime)
                    .ToList();
            }
            return View();
        }

    }
}
