using BakeryOrderSystem.Data;
using BakeryOrderSystem.Helpers;
using BakeryOrderSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BakeryOrderSystem.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Администратор")
            {
                TempData["Error"] = "У вас нет прав для доступа к данному разделу.";
                return RedirectToAction("Index", "Home");
            }

            var users = await _context.Users
                .Include(u => u.Role)
                .ToListAsync();

            return View(users);
        }
        public IActionResult Create()
        {
            ViewBag.Roles = new SelectList(_context.Roles, "Id", "Name");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(User user)
        {
            ModelState.Remove("Role");

            if (ModelState.IsValid)
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Roles = new SelectList(_context.Roles, "Id", "Name", user.RoleId);

            return View(user);
        }
        public async Task<IActionResult> Delete(int id)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Администратор")
            {
                TempData["Error"] = "У вас нет прав для доступа к данному разделу.";
                return RedirectToAction("Index", "Home");
            }

            var currentUserName = HttpContext.Session.GetString("UserName");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                TempData["Error"] = "Пользователь уже был удален.";
                return RedirectToAction(nameof(Index));
            }

            if (user.FullName == currentUserName)
            {
                TempData["Error"] = "Нельзя удалить текущего пользователя.";
                return RedirectToAction(nameof(Index));
            }

            var logs = await _context.LoginLogs
                .Where(l => l.UserId == id)
                .ToListAsync();

            if (logs.Any())
            {
                _context.LoginLogs.RemoveRange(logs);
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Edit(int id)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Администратор")
            {
                TempData["Error"] = "У вас нет прав для доступа к данному разделу.";
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Roles = new SelectList(_context.Roles, "Id", "Name", user.RoleId);

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(User user)
        {
            ModelState.Remove("Role");

            if (ModelState.IsValid)
            {
                _context.Users.Update(user);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Roles = new SelectList(_context.Roles, "Id", "Name", user.RoleId);

            return View(user);
        }
    }
}