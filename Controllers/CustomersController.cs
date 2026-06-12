using BakeryOrderSystem.Data;
using BakeryOrderSystem.Helpers;
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
                return RedirectToAction("Login", "Account");
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
    }
}