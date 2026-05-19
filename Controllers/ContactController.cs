using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniShare.Data;

namespace UniShare.Controllers
{
    public class ContactController : Controller
    {
        private readonly UniShareDbContext _context;
        public ContactController(UniShareDbContext context)
        {
            _context = context;
        }

        // GET: ContactController
        public async Task<IActionResult> Index()
        {
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
            return View(adminUser);
        }

    }
}
