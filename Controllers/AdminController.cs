using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using UniShare.Data;
using UniShare.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace UniShare.Controllers
{
    public class AdminController : Controller
    {
        private readonly UniShareDbContext _context;
        public AdminController(UniShareDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("UserRole") == "Admin";
        }

        // Block non-admin access
        private IActionResult NoAccess()
        {
            TempData["Error"] = "Access denied. Admin only.";
            return RedirectToAction("Login", "Account");
        }

        // System Overview Dashboard
        public async Task<IActionResult> SystemOverview()
        {
            if (!IsAdmin())
            {
                return NoAccess();
            }

            try
            {
                ViewBag.OnlineUsers = await _context.Users.CountAsync(u => u.AccountStatus == "Active");
                ViewBag.ActiveRides = await _context.Rides.CountAsync(r => r.RideStatus == "Active");
                ViewBag.TodayReports = await _context.Reports.CountAsync(r => r.CreatedAt.Date == DateTime.Today);
                ViewBag.DisputedRides = await _context.Rides.CountAsync(r => r.RideStatus == "Disputed");
            }
            catch
            {
                ViewBag.Error = "Unable to load system data.Please try again later.";
            }
            return View();
        }

        // Manage All Users
        public async Task<IActionResult> UserManagement(string search = "")
        {
            if (!IsAdmin())
            {
                return NoAccess();
            }
            var users = _context.Users.Where(u=>u.Role == "Driver"|| u.Role == "Passenger").AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                users = users.Where(u => u.UserName.Contains(search) || u.Email.Contains(search));
            }
            ViewBag.Search = search;
            return View(await users.ToListAsync());

        }

        // Edit User Status
        public async Task<IActionResult> EditUserStatus(int id)
        {
            if (!IsAdmin())
            {
                return NoAccess();
            }
            var user = await _context.Users.FindAsync(id);
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditUserStatus(int id, string newStatus, string reason)
        {
            if (!IsAdmin())
            {
                return NoAccess();
            }
            var user = await _context.Users.FindAsync(id);
            if(user == null)
            {
                return NotFound();
            }

            // prevent suspend if user has active ride
            bool hasActiveRide = await _context.Rides.AnyAsync(r => r.DriverId == id && r.RideStatus == "Active");
            if(hasActiveRide && newStatus != "Active")
            {
                TempData["Error"] = "Cannot suspend user during an active ride. Please wait until the ride is completed.";
                return RedirectToAction("UserManagement");
            }

            user.AccountStatus = newStatus; ;

            // Log admin action
            await _context.systemAuditLogs.AddAsync(new SystemAuditLog
            {
                AdminUserId = HttpContext.Session.GetInt32("UserId") ?? 0,
                ActionTaken = $"Changed user status to {newStatus}",
                AffectedEntity = $"User: {user.UserName}",
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "User status updated successfully.";
            return RedirectToAction("UserManagement");

        }

        // Manage All Rides
        public async Task<IActionResult> RideManagement(string searchRideId = "")
        {
            if (!IsAdmin())
            {
                return NoAccess() ;
            }

            var rides = _context.Rides.Include(r => r.Driver).OrderByDescending(r => r.RideDate).AsQueryable();

            if (!string.IsNullOrEmpty(searchRideId))
            {
                rides = rides.Where(r => r.RideId.ToString() == searchRideId);
            }

            ViewBag.SearchRideId = searchRideId;
            var ridesList = await rides.ToListAsync();
            return View(ridesList);
        }

        public async Task<IActionResult> RideDetails(int id)
        {
            if (!IsAdmin())
            {
                return NoAccess();
            }

            var ride = await _context.Rides.Include(r => r.Driver).FirstOrDefaultAsync(r => r.RideId == id);
            if (ride == null)
            {
                return NotFound();
            }

            // Link report to this ride
            var reports = await _context.Reports.Where(r => r.RelatedRideId == id).ToListAsync();
            ViewBag.DisputeReports = reports;
            ViewBag.DbContext = _context;
            return View(ride);
        }

        [HttpPost]
        public async Task<IActionResult> ResolveDispute(int rideId, string resolution)
        {
            if (!IsAdmin())
            {
                return NoAccess();
            }

            var ride = await _context.Rides.FindAsync(rideId);
            if (ride == null)
            {
                return NotFound();
            }
            ride.RideStatus = "DisputeResolved";

            ride.DisputeResolution = resolution;

            var allRideReports = await _context.Reports.Where(r => r.RelatedRideId == rideId).ToListAsync();
            foreach (var rp in allRideReports)
            {
                rp.ReportStatus = "Resolved";
            }
            if (resolution == "Flag Driver for Review")
            {
                TempData["FlaggedDriver"] = "Yes";
            }
            if (resolution == "Flag Passenger for Review")
            {
                TempData["FlaggedPassenger"] = "Yes";
            }

            await _context.systemAuditLogs.AddAsync(new SystemAuditLog
            {
                AdminUserId = HttpContext.Session.GetInt32("UserId") ?? 0,
                ActionTaken = $"Dispute resolved: {resolution}",
                AffectedEntity = $"Ride ID {rideId}"
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Dispute resolved and logged";
            return RedirectToAction("RideManagement");
        }

        // Handle Reports & Flags
        public async Task<IActionResult> ReportsFlags(string search = "")
        {
            if (!IsAdmin())
            {
                return NoAccess();
            }
            var reports = _context.Reports.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                reports = reports.Where(r =>
                    r.ReportId.ToString().Contains(search) ||
                    r.RelatedRideId.ToString().Contains(search) ||
                    r.ReportReason.Contains(search)
                );
            }

            var sortedReports = await reports.OrderBy(r => r.ReportStatus == "Pending" ? 0 : 1).ToListAsync();
            ViewBag.Search = search;
            return View(sortedReports);

        }


    }
}
