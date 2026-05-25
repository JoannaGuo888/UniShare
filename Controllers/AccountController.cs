using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniShare.Data;
using UniShare.Models;

namespace UniShare.Controllers
{
    public class AccountController : Controller
    {
        private readonly UniShareDbContext _context;
        public AccountController(UniShareDbContext context)
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

        // Register Page
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            // Check if email already exists
            bool emailExists = await _context.Users.AnyAsync(u => u.Email == user.Email && u.Role == user.Role);
            if (emailExists)
            {
                TempData["Error"] = $"This email is already registered as {user.Role}!";
                return View(user);
            }

            user.PasswordHash = ComputeSha256Hash(user.PasswordHash);
            user.AccountStatus = "Active";
            user.JoinDate = DateTime.UtcNow.AddHours(1);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Account created successfully! Please login.";
            return RedirectToAction("Login");
        }

        // Login Page

        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string role)
        {
            string hashed = ComputeSha256Hash(password);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.PasswordHash == hashed && u.Role == role );
            if (user == null)
            {
                TempData["Error"] = "Invalid username or password or role!"; // TempData to prevent form resubmission
                return View();
            }

            if (user.AccountStatus == "Banned")
            {
                TempData["Error"] = "Your account is BANNED. Cannot login.";
                return View();
            }

            // Session for Role & User ID
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserName", user.UserName);
            return RedirectToAction("Dashboard", "Home");
        } 

        // Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // My Profile
        public IActionResult MyProfile()
        {
            int id = (int)HttpContext.Session.GetInt32("UserId");
            var user = _context.Users.Find(id);
            return View(user);
        }

        // Edit Page
        public async Task<IActionResult> EditProfile(int? id)
        {
            if(id == null)
            {
                return NotFound("A user ID is required!");
            }
            var user= await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound("User not found");
            }
            return View(user);
        }

        // Edit Profile
        [HttpPost]
        public async Task<IActionResult> EditProfile(User updated)
        {
            var user = _context.Users.Find(updated.UserId);
            if (user == null)
            {
                return NotFound("Cannot find user");

            }
            user.UserName = updated.UserName;
            user.PhoneNumber = updated.PhoneNumber;
            user.HomeAddress = updated.HomeAddress;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("MyProfile");
        }

        // Password Hashing
        private string ComputeSha256Hash(string raw)  
        {
            using SHA256 sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

    }
}
