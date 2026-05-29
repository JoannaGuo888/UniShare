using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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


        public async Task AutoCancelExpiredRequests()
        {
            var expired = await _context.RideRequests.Include(r => r.Ride).Where(r => r.RequestStatus == "New" && DateTime.UtcNow.AddHours(1).Subtract(r.RequestCreatedTime).TotalMinutes > 30).ToListAsync();
            foreach (var req in expired)
            {
                req.RequestStatus = "CancelledByAdmin";
                // Release seat
                if (req.Ride != null)
                {
                    req.Ride.AvailableSeats++;
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task<IActionResult> Dashboard()
        {
            await AutoCancelExpiredRequests();
            var role = HttpContext.Session.GetString("UserRole");
            ViewBag.UserRole = role;
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            if (role == "Driver")
            {
                int driverId = HttpContext.Session.GetInt32("UserId") ?? 0;
                ViewBag.TodayRides = await _context.Rides
                    .Where(r => r.DriverId == driverId
                             && r.RideDate.Date == DateTime.UtcNow.AddHours(1).Date
                             && (r.RideStatus == "Upcoming" || r.RideStatus == "Active"))
                    .OrderBy(r => r.RideTime)
                    .ToListAsync();
            }

            if (role == "Passenger")
            {
                int passengerId = HttpContext.Session.GetInt32("UserId") ?? 0;
                ViewBag.FutureRequests = await _context.RideRequests
                    .Include(r => r.Ride)
                    .Include(r => r.Driver)
                    .Where(r => r.PassengerId == passengerId
                             && r.Ride != null
                             && r.Ride.RideDate.Date >= DateTime.UtcNow.AddHours(1).Date
                             && r.Ride.RideStatus == "Upcoming")
                    .OrderBy(r => r.Ride.RideDate)
                    .ThenBy(r => r.Ride.RideTime)
                    .ToListAsync();
            }
            return View();
        }

    }
}
