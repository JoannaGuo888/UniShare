using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniShare.Data;
using UniShare.Models;

namespace UniShare.Controllers
{
    
    public class RideController : Controller
    {
        private readonly UniShareDbContext _context;
        public RideController(UniShareDbContext context)
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

        // Get logged-in Driver ID from Session
        private int GetCurrentDriverId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        // Auto-Mark expired rides (only if 2 hours past time and still upcoming)
        private async Task MarkExpiredRides() 
        {
            // Get all rides that are upcoming and their date/time is in the past
            var upcomingRides = await _context.Rides.Where(r => r.RideStatus == "Upcoming").ToListAsync();
            var expiredRides = upcomingRides.Where(r => r.RideDate.Add(r.RideTime).AddHours(2)< DateTime.UtcNow.AddHours(1));
            
            foreach(var ride  in expiredRides)
            {
                ride.RideStatus = "Expired";
            }
            await _context.SaveChangesAsync();
        }



        // Driver Calendar Page

        public IActionResult DriverCalendar()
        {
            int driverId = GetCurrentDriverId();
            ViewBag.UpcomingRideDates = _context.Rides
               .Where(r => r.DriverId == driverId && r.RideStatus == "Upcoming")
               .Select(r => r.RideDate.Date)
               .Distinct()
               .ToList();

            ViewBag.DriverId = driverId;
            return View();
        }

        // Load rides for selected date
        public async Task<IActionResult> RidesByDate(DateTime date)
        {
            await MarkExpiredRides();
            int driverId = GetCurrentDriverId();
            var rides = await _context.Rides
                .Where(r=>r.DriverId == driverId && r.RideDate.Date==date.Date)
                .ToListAsync();
            ViewBag.SelectedDate = date;
            return View(rides);


        }

        // Create Ride
        public IActionResult CreateRide(DateTime? date)
        {
            var ride = new Ride();
            if (date.HasValue)
            {
                ride.RideDate = date.Value; // lock the selected date
            }
            return View(ride);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRide(Ride ride)
        {
            if (IsUserSuspended())
            {
                TempData["Error"] = "Your account is SUSPENDED. You cannot use this feature.";
                return RedirectToAction("Dashboard", "Home");
            }
            var lockDate = ride.RideDate;

            DateTime fullRideTime = lockDate + ride.RideTime;
            if(fullRideTime <= DateTime.UtcNow.AddHours(1))
            {
                ModelState.AddModelError("RideTime", "Please select a future time only!");
                return View(ride);
            }
            
            if (!ModelState.IsValid)
            {
                return View(ride);
            }
            ride.DriverId = GetCurrentDriverId();
            ride.RideStatus = "Upcoming";
            ride.RideDate = lockDate;  // keep date locked
            _context.Rides.Add(ride);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Ride successfully created";
            return RedirectToAction("RidesByDate", new {date=ride.RideDate});


        }

        // View Single Ride Details
        public async Task<IActionResult> ViewRide(int id)
        {
            await MarkExpiredRides();
            var ride = await _context.Rides.FindAsync(id);
            if (ride == null)
            {
                return NotFound("Ride not found!");

            }
            // Past/Active Ride = Edit button disabled
            ViewBag.CanEdit = ride.RideStatus == "Upcoming";
            return View(ride);

        }

        // Edit Ride (only upcoming allowed)
        public async Task<IActionResult> EditRide(int id)
        {
            var ride = await _context.Rides.FindAsync(id);
            
            if(ride == null || ride.RideStatus != "Upcoming")
            {
                TempData["Error"] = "Cannot edit: Only upcoming rides can be edited";
                return RedirectToAction("ViewRide", new { id });
            }
            return View(ride);
        }

        [HttpPost]
        public async Task<IActionResult> EditRide(Ride ride)
        {
            if (IsUserSuspended())
            {
                TempData["Error"] = "Your account is SUSPENDED. You cannot use this feature.";
                return RedirectToAction("Dashboard", "Home");
            }
            var existing = await _context.Rides.FindAsync(ride.RideId);
            if(existing == null || existing.RideStatus != "Upcoming")
            {
                TempData["Error"] = "Edit failed: Ride not found or is no longer upcoming";
                return RedirectToAction("DriverCalendar");
            }
            if (ride.RideDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("RideDate", "Date cannot be in the past. Use today or future.");
                return View(ride);
            }
            DateTime fullRideDateTime = ride.RideDate + ride.RideTime;
            if(fullRideDateTime <= DateTime.UtcNow.AddHours(1))
            {
                ModelState.AddModelError("RideTime", "Please select a future time only!");
                return View(ride);
            }

            // Save old values for email
            string oldFrom = existing.StartLocation;
            string oldTo = existing.Destination;
            DateTime oldDate = existing.RideDate;
            TimeSpan oldTime = existing.RideTime;

            // Update ride
            existing.StartLocation = ride.StartLocation; 
            existing.Destination = ride.Destination;
            existing.RideDate = ride.RideDate;
            existing.RideTime = ride.RideTime;
            existing.AvailableSeats = ride.AvailableSeats;
            existing.CostPerSeat = ride.CostPerSeat;

            await _context.SaveChangesAsync();

            // Email notification for edit

            var passengers = await _context.RideRequests.Include(r => r.Passenger).Where(r => r.RideId == ride.RideId).Select(r => r.Passenger.Email).ToListAsync();
            if(passengers.Any())
            {
                string to = string.Join(",", passengers);
                string subject = "Notice: Your ride details have been updated";
                string body = $"Hello! The driver has updated your ride details: OLD: {oldFrom} -> {oldTo} on {oldDate:dd/MM/yyyy} at {oldTime} | NEW: {existing.StartLocation} -> {existing.Destination} on {existing.RideDate:dd/MM/yyyy} at {existing.RideTime}. Thank you.";

                TempData["MailTo"] = to;
                TempData["MailSubject"] = subject;
                TempData["MailBody"] = body;

            }

            TempData["Success"] = "Ride updated successfully";
            return RedirectToAction("ViewRide", new { id = ride.RideId });


        }

        // Delete Ride
        public async Task<IActionResult> DeleteRide(int id)
        {
            if (IsUserSuspended())
            {
                TempData["Error"] = "Your account is SUSPENDED. You cannot use this feature.";
                return RedirectToAction("Dashboard", "Home");
            }
            var ride = await _context.Rides.FirstOrDefaultAsync(r => r.RideId == id);

            if(ride == null )
            {
                TempData["Error"] = "Ride not found.";
                return RedirectToAction("DriverCalendar");
            }

            if (ride.RideStatus != "Upcoming")
            {
                TempData["Error"] = "Cannot delete: Only upcoming rides can be removed.";
                return RedirectToAction("DriverCalendar");

            }

            int currentDriverId = GetCurrentDriverId();
            if(ride.DriverId != currentDriverId)
            {
                TempData["Error"] = "You can only delete your own rides.";
                return RedirectToAction("DriverCalendar");
            }


            // get all passengers of this ride
            var passengers = await _context.RideRequests.Include(r=>r.Passenger).Where(r => r.RideId == id).Select(r => r.Passenger.Email).ToListAsync();

            // Save ride details for email before deleting
            string rideFrom = ride.StartLocation;
            string rideTo = ride.Destination;
            DateTime rideDate = ride.RideDate;
            TimeSpan rideTime = ride.RideTime;

            // Delete all related requests
            var requests = await _context.RideRequests.Where(r=>r.RideId == id).ToListAsync();
            _context.RideRequests.RemoveRange(requests);
            _context.Rides.Remove(ride);
            await _context.SaveChangesAsync();
            // Email Notification for passengers enrolled
            if (passengers.Any())
            {
                string to = string.Join(",", passengers);
                string subject = $"URGENT: Your ride was cancelled by driver";
                string body = $"Hello, Your ride from {ride.StartLocation} to {ride.Destination} on {ride.RideDate:dd/MM/yyyy} at {ride.RideTime} has been cancelled by the driver.";

                TempData["MailTo"] = to;
                TempData["MailSubject"] = subject;
                TempData["MailBody"] = body;
            }

            TempData["Success"] = "Ride and all related requests were deleted successfully. Passengers have been notified";

            return RedirectToAction("RidesByDate", new {date = ride.RideDate});
        }

        // Start Ride
        public async Task<IActionResult> StartRide(int id)
        {
            if (IsUserSuspended())
            {
                TempData["Error"] = "Your account is SUSPENDED. You cannot use this feature.";
                return RedirectToAction("Dashboard", "Home");
            }
            var ride = await _context.Rides.FindAsync(id);
            if (ride == null)
            {
                return NotFound();
            }
            DateTime scheduledStart = ride.RideDate.Add(ride.RideTime);
            // Can't start before 30 mins of scheduled time
            if(DateTime.UtcNow.AddHours(1) < scheduledStart.AddMinutes(-30))
            {
                TempData["Error"] = $"You can only start this ride 30 minutes before its scheduled time {scheduledStart:HH:mm}.";
                return RedirectToAction("ViewRide", new { id });
            }
            ride.RideStatus = "Active";
            // Auto cancel all pending requests for this ride
            var reqctrl = new RequestController(_context);
            await reqctrl.AutoCancelWhenRideStarted(id);
            // Get accepted passengers for email
            var acceptedPassengers = await _context.RideRequests.Include(r => r.Passenger).Where(r => r.RideId == id && r.RequestStatus == "Accepted").Select(r => r.Passenger.Email).ToListAsync();
            // Send email notification

            if (acceptedPassengers.Any())
            {
                string to = string.Join(",", acceptedPassengers);
                string subject = "Your Ride Has Started!";
                string body = $"Hello! Your ride from {ride.StartLocation} to {ride.Destination} on {ride.RideDate:dd/MM/yyyy} at {ride.RideTime} has STARTED.\n Thank you for using UniShare!";

                // Store in TempData for your mail system
                TempData["MailTo"] = to;
                TempData["MailSubject"] = subject;
                TempData["MailBody"] = body;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Ride started!";
            return RedirectToAction("RidesByDate", new {date=ride.RideDate});
        }

        // Finish Ride
        public async Task<IActionResult> FinishRide(int id)
        {
            if (IsUserSuspended())
            {
                TempData["Error"] = "Your account is SUSPENDED. You cannot use this feature.";
                return RedirectToAction("Dashboard", "Home");
            }
            var ride = await _context.Rides.FindAsync(id);
            if(ride == null)
            {
                return NotFound();
            }
            ride.RideStatus = "Completed";
            await _context.SaveChangesAsync();
            return RedirectToAction("RidesByDate", new { date=ride.RideDate });

        }


    }
}
