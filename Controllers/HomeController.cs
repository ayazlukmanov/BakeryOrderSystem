using BakeryOrderSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BakeryOrderSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userName = HttpContext.Session.GetString("UserName");

            if (userName == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var role = HttpContext.Session.GetString("Role");

            if (role == "Администратор" || role == "Менеджер")
            {
                ViewBag.ShowStatistics = true;
                ViewBag.ProductsCount = await _context.Products.CountAsync();
                ViewBag.CustomersCount = await _context.Customers.CountAsync();
                ViewBag.OrdersCount = await _context.Orders.CountAsync();
                ViewBag.TotalRevenue = await _context.Orders.SumAsync(o => o.TotalPrice);
            }
            else
            {
                ViewBag.ShowStatistics = false;
            }

            return View();
        }
    }
}