using BakeryOrderSystem.Data;
using BakeryOrderSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BakeryOrderSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string login, string password)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.Login == login &&
                    u.Password == password);

            if (user == null)
            {
                ViewBag.Error = "Неверный логин или пароль";
                return View();
            }

            var openedLogs = await _context.LoginLogs
                .Where(l => l.UserId == user.Id && l.LogoutTime == null)
                .ToListAsync();

            foreach (var log in openedLogs)
            {
                log.LogoutTime = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("Role", user.Role.Name);

            var loginLog = new LoginLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                LoginTime = DateTime.Now
            };

            _context.LoginLogs.Add(loginLog);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetInt32("LoginLogId", loginLog.Id);

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            var loginLogId = HttpContext.Session.GetInt32("LoginLogId");

            if (loginLogId != null)
            {
                var log = await _context.LoginLogs.FindAsync(loginLogId.Value);

                if (log != null && log.LogoutTime == null)
                {
                    log.LogoutTime = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }

            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }
    }
}
