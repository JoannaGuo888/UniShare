using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;
using UniShare.Data;
using UniShare.Models;

namespace UniShare.Controllers
{
    public class RequestController : Controller
    {
        private readonly UniShareDbContext _context;
        public RequestController(UniShareDbContext context)
        {
            _context = context;
        }

        private bool IsUserSuspended()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return false;

            var user = _context.Users.Find(userId.Value);
            if (user == null) return false;

            return user.AccountStatus == "Suspended";
        }
        private int CurrentUserId => HttpContext.Session.GetInt32("UserId") ?? 0;
        private string CurrentRole => HttpContext.Session.GetString("UserRole") ?? "";

        private async Task MarkExpiredRides()
        {
            // Get all rides that are upcoming and their date/time is in the past
            var upcomingRides = await _context.Rides.Where(r => r.RideStatus == "Upcoming").ToListAsync();
            var expiredRides = upcomingRides.Where(r => r.RideDate.Add(r.RideTime).AddHours(2) < DateTime.UtcNow.AddHours(1));

            foreach (var ride in expiredRides)
            {
                ride.RideStatus = "Expired";
            }
            await _context.SaveChangesAsync();
        }

        private async Task UpdateRequestsForExpiredRides()
        {
            var expiredRidesIds = await _context.Rides.Where(r => r.RideStatus == "Expired").Select(r => r.RideId).ToListAsync();
            if(!expiredRidesIds.Any()) { return; }

            // Find requests for expired rides
            var reqs = await _context.RideRequests.Where(r => expiredRidesIds.Contains(r.RideId) && (r.RequestStatus == "New" || r.RequestStatus == "Accepted")).ToListAsync();
            foreach(var req in reqs)
            {
                req.RequestStatus = "RideExpired";
            }
            await _context.SaveChangesAsync();
        }

        private string GetPriority(string reason)  
        {
            if (reason.Contains("No-show") || reason.Contains("Rude") || reason.Contains("Unsafe"))
                return "High";
            if (reason.Contains("Late") || reason.Contains("Communication") || reason.Contains("Changed plans"))
                return "Normal";
            return "Low";
        }

        // Driver: All Requests Received
        public async Task<IActionResult> AllRequestsReceived()
        {
            await UpdateRequestsForExpiredRides();
            int driverId = CurrentUserId;
            var requests = await _context.RideRequests.Include(r => r.Ride).Include(r => r.Passenger).Where(r => r.DriverId == driverId).OrderByDescending(r => r.RequestCreatedTime).ToListAsync();
            return View(requests);
        }

        // Accept Request
        public async Task<IActionResult> AcceptRequest(int id)
        {
            if (IsUserSuspended())
            {
                TempData["Error"] = "Your account is SUSPENDED. You cannot use this feature.";
                return RedirectToAction("Dashboard", "Home");
            }
            var req = await _context.RideRequests.Include(r => r.Ride).FirstOrDefaultAsync(r => r.RequestId == id);
            if (req == null)
            {
                return NotFound();
            }
            if (req.Ride == null || req.Ride.RideStatus == "Expired")
            {
                TempData["Error"] = "Cannot accept: Ride expired.";
                return RedirectToAction("AllRequestsReceived");
            }
            // Update request status
            req.RequestStatus = "Accepted";


            await _context.SaveChangesAsync();
            TempData["Success"] = "Request accepted. Seat reserved.";
            return RedirectToAction("AllRequestsReceived");
        }

        // Decline Request
        public async Task<IActionResult> DeclineRequest(int id)
        {
            if (IsUserSuspended())
            {
                TempData["Error"] = "Your account is SUSPENDED. You cannot use this feature.";
                return RedirectToAction("Dashboard", "Home");
            }
            var req = await _context.RideRequests.Include(r => r.Ride).FirstOrDefaultAsync(r => r.RequestId == id);
            if(req == null)
            {
                return NotFound();
            }
            if (req.Ride == null || req.Ride.RideStatus == "Expired")
            {
                TempData["Error"] = "Cannot decline: Ride expired.";
                return RedirectToAction("AllRequestsReceived");
            }
            req.RequestStatus = "Declined";

            // Release seat
            if (req.Ride != null)
            {
                req.Ride.AvailableSeats++;
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "Request declined. Seat released.";
            return RedirectToAction("AllRequestsReceived");
        }

        // Driver Cancel Accepted Request
        public async Task<IActionResult> CancelAcceptedRequest(int id)
        {
            if (IsUserSuspended())
            {
                TempData["Error"] = "Your account is SUSPENDED. You cannot use this feature.";
                return RedirectToAction("Dashboard", "Home");
            }
            var req = await _context.RideRequests.Include(r => r.Ride).Include(r=>r.Passenger).FirstOrDefaultAsync(r => r.RequestId == id);
            if (req == null)
            {
                return NotFound();
            }
            if (req.Ride == null || req.Ride.RideStatus == "Expired")
            {
                TempData["Error"] = "Cannot cancel: Ride expired.";
                return RedirectToAction("AllRequestsReceived");
            }

            string passengerEmail = null;
            // Save info for email before changes
            if (req.Passenger != null)
            {
                passengerEmail = req.Passenger.Email;
            }
            string rideFrom = "Unknown";
            string rideTo = "Unknown";
            DateTime rideDate = DateTime.UtcNow.AddHours(1);
            TimeSpan rideTime = TimeSpan.Zero;

            if (req.Ride != null)
            {
                rideFrom = req.Ride.StartLocation;
                rideTo = req.Ride.Destination;
                rideDate = req.Ride.RideDate;
                rideTime = req.Ride.RideTime;
            }
            req.RequestStatus = "CancelledByDriver";

            // Release seat
            if(req.Ride != null)
            {
                req.Ride.AvailableSeats++;
                req.Ride.RideStatus = "Upcoming";
            }


            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(passengerEmail))
            {
                TempData["MailTo"] = passengerEmail;
                TempData["MailSubject"] = "URGENT: Your accepted request of ride was cancelled by driver";
                TempData["MailBody"] = $"Hello! Your accepted request of ride from {rideFrom} to {rideTo} on {rideDate:dd/MM/yyyy} at {rideTime} was cancelled by the driver.";
            }

            TempData["Success"] = "Request cancelled, seat released.";
            return RedirectToAction("AllRequestsReceived");


        }
        

        // 30 mins auto cancel (Admin System Logic)
        public async Task AutoCancelExpiredRequests()
        {
            var expired = await _context.RideRequests.Include(r => r.Ride).Where(r => r.RequestStatus == "New" && DateTime.UtcNow.AddHours(1).Subtract(r.RequestCreatedTime).TotalMinutes > 30).ToListAsync();
            foreach(var req in expired)
            {
                req.RequestStatus = "CancelledByAdmin";
                // Release seat
                if(req.Ride != null )
                {
                    req.Ride.AvailableSeats++;
                }
            }
            await _context.SaveChangesAsync();
        }

        // auto cancel all pending requests if ride started
        public async Task AutoCancelWhenRideStarted(int rideId)
        {
            var pending = await _context.RideRequests.Include(r => r.Ride).Where(r => r.RideId == rideId && r.RequestStatus == "New").ToListAsync();
            foreach( var req in pending)
            {
                req.RequestStatus = "CancelledByAdmin";
                if (req.Ride != null)
                {
                    req.Ride.AvailableSeats++;
                }
            }
            await _context.SaveChangesAsync();
        }

        // Passenger: Public Ride Board
        public async Task<IActionResult> PublicRideBoard(string from, string to, DateTime? date)
        {
            await AutoCancelExpiredRequests();
            await MarkExpiredRides();
            var allRides =  _context.Rides.Include(r => r.Driver).Where(r => r.RideStatus == "Upcoming" && r.AvailableSeats > 0).AsQueryable();
            // Search filter
            if (!string.IsNullOrEmpty(from))
            {
                allRides = allRides.Where(r => r.StartLocation.Contains(from));
            }

            if (!string.IsNullOrEmpty(to))
            {
                allRides = allRides.Where(r => r.Destination.Contains(to));
            }

            if (date.HasValue)
            {
                allRides = allRides.Where(r => r.RideDate.Date == date.Value.Date);
            }

            // Sort by time
            var availableRides = await allRides.ToListAsync();
            var result = availableRides.Where(r => r.RideDate.Add(r.RideTime) > DateTime.UtcNow.AddHours(1)).OrderBy(r => r.RideDate).ThenBy(r => r.RideTime).ToList();
            ViewBag.SearchFrom = from;
            ViewBag.SearchTo = to;
            ViewBag.SearchDate = date;
            return View(result);

        }

        // Passenger: Send Request Form
        public IActionResult SendRequest(int rideId)
        {
            ViewBag.RideId = rideId;
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> SendRequest(string message, int rideId)
        {
            if (IsUserSuspended())
            {
                TempData["Error"] = "Your account is SUSPENDED. You cannot use this feature.";
                return RedirectToAction("Dashboard", "Home");
            }
            if (CurrentUserId == 0)
            {
                TempData["Error"] = "You must be logged in to send a request";
                return RedirectToAction("Login", "Account");
            }
            var ride = await _context.Rides.Include(r => r.Driver).FirstOrDefaultAsync(r => r.RideId == rideId);
            if (ride == null)
            {
                TempData["Error"] = "Ride not found";
                return RedirectToAction("PublicRideBoard");
            }
            if (ride.RideStatus == "Expired")
            {
                TempData["Error"] = "Cannot send request: Ride has expired.";
                return RedirectToAction("PublicRideBoard");
            }

            // No seats left check
            if (ride.AvailableSeats <= 0)
            {
                TempData["Error"] = "Ride no longer available due to capacity";
                return RedirectToAction("PublicRideBoard");
            }

            bool alreadyHasRequest = await _context.RideRequests
               .AnyAsync(r => r.PassengerId == CurrentUserId
                   && r.RideId == rideId
                   && (r.RequestStatus == "New" || r.RequestStatus == "Accepted"));

            if (alreadyHasRequest)
            {
                TempData["Error"] = "You already have a pending or accepted request for this ride!";
                return RedirectToAction("PublicRideBoard");
            }
            // Create new request
            var newReq = new RideRequest
            {
                RideId = rideId,
                DriverId = ride.DriverId,
                PassengerId = CurrentUserId,
                Message = message,
                RequestStatus = "New",
                RequestCreatedTime = DateTime.UtcNow.AddHours(1)

            };

            ride.AvailableSeats--;

            _context.RideRequests.Add(newReq);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Request sent. Seat reserved for 30 minutes.";

            return RedirectToAction("MySentRequests");

        }

        // Passenger: All Requests Sent
        public async Task<IActionResult> MySentRequests()
        {
            await UpdateRequestsForExpiredRides();
            var myRequests = await _context.RideRequests.Include(r=>r.Ride).Include(r=>r.Driver).Where(r=>r.PassengerId == CurrentUserId).OrderByDescending(r=>r.RequestCreatedTime).ToListAsync();
            return View(myRequests);
        }

        // Passenger: Cancel Pending Request
        public async Task<IActionResult> CancelSentRequest(int id)
        {
            if (IsUserSuspended())
            {
                TempData["Error"] = "Your account is SUSPENDED. You cannot use this feature.";
                return RedirectToAction("Dashboard", "Home");
            }
            var req = await _context.RideRequests.Include(r=>r.Ride).FirstOrDefaultAsync(r=>r.RequestId== id);
            if(req == null || req.PassengerId != CurrentUserId)
            {
                return NotFound();
            }

            // Only cancel if still New
            if(req.RequestStatus == "New")
            {
                req.RequestStatus = "CancelledByPassenger";

                // Release seat
                if(req.Ride!= null)
                {
                    req.Ride.AvailableSeats++;

                }
                await _context.SaveChangesAsync();
                TempData["Success"] = "Request cancelled successfully.";

            }
            return RedirectToAction("MySentRequests");
        }

        [HttpPost]
        public async Task<IActionResult> ReportRide(int rideId, string reportReason, string customReason)
        {
            if (IsUserSuspended())
            {
                TempData["Error"] = "Your account is SUSPENDED. You cannot use this feature.";
                return RedirectToAction("Dashboard", "Home");
            }

            var ride = await _context.Rides.FindAsync(rideId);
            if (ride == null) return NotFound();

            string finalReason = string.IsNullOrEmpty(customReason)
                ? reportReason
                : $"Other: {customReason}";

            string priority = GetPriority(reportReason);

            ride.RideStatus = "Disputed";

            await _context.Reports.AddAsync(new Report
            {
                RelatedRideId = rideId,
                SubjectUserId = ride.DriverId,
                ReporterUserId = CurrentUserId,
                ReportReason = finalReason,
                ReportStatus = "Pending",
                Priority = priority,
                CreatedAt = DateTime.UtcNow.AddHours(1)
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Ride reported! Admin will review the dispute.";
            return RedirectToAction("MySentRequests");
        }

        [HttpPost]
        public async Task<IActionResult> ReportPassenger(int requestId, string reportReason, string customReason)
        {
            if (IsUserSuspended())
            {
                TempData["Error"] = "Your account is SUSPENDED. You cannot use this feature.";
                return RedirectToAction("Dashboard", "Home");
            }

            var req = await _context.RideRequests
                .Include(r => r.Ride)
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (req == null || req.Ride == null) 
            {
                return NotFound();
            }

            string finalReason = string.IsNullOrEmpty(customReason)
                ? reportReason
                : $"Other: {customReason}";

            string priority = GetPriority(reportReason);

            req.Ride.RideStatus = "Disputed";

            await _context.Reports.AddAsync(new Report
            {
                RelatedRideId = req.RideId,
                ReporterUserId = CurrentUserId,
                SubjectUserId = req.PassengerId,
                ReportReason = finalReason,
                ReportStatus = "Pending",
                Priority = priority,
                CreatedAt = DateTime.UtcNow.AddHours(1)
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Passenger reported to admin.";
            return RedirectToAction("AllRequestsReceived");
        }

    }
}
