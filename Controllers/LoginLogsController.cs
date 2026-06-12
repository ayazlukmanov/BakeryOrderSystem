using BakeryOrderSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BakeryOrderSystem.Controllers
{
    public class LoginLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoginLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Администратор")
            {
                return RedirectToAction("Login", "Account");
            }

            var logs = await _context.LoginLogs
                .OrderByDescending(l => l.LoginTime)
                .ToListAsync();

            return View(logs);
        }
    }
}