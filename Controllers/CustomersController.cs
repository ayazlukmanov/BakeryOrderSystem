using BakeryOrderSystem.Data;
using BakeryOrderSystem.Helpers;
using BakeryOrderSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BakeryOrderSystem.Controllers
{
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search)
        {
            var userName = HttpContext.Session.GetString("UserName");

            if (userName == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var role = HttpContext.Session.GetString("Role");

            if (!RoleCheck.IsAdminOrManager(role))
            {
                TempData["Error"] = "У вас нет прав для доступа к данному разделу.";
                return RedirectToAction("Index", "Home");
            }

            var customers = _context.Customers.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                customers = customers.Where(c =>
                    c.FullName.Contains(search) ||
                    c.Phone.Contains(search) ||
                    c.Email.Contains(search));
            }

            ViewBag.Search = search;

            return View(await customers.ToListAsync());
        }

        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("Role");

            if (!RoleCheck.IsAdminOrManager(role))
            {
                TempData["Error"] = "У вас нет прав для доступа к данному разделу.";
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Customer customer)
        {
            var role = HttpContext.Session.GetString("Role");

            if (!RoleCheck.IsAdminOrManager(role))
            {
                TempData["Error"] = "У вас нет прав для доступа к данному разделу.";
                return RedirectToAction("Index", "Home");
            }

            customer.FullName ??= "";
            customer.Phone ??= "";
            customer.Email ??= "";
            customer.Address ??= "";

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}