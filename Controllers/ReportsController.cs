using BakeryOrderSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BakeryOrderSystem.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.ProductsCount = await _context.Products.CountAsync();
            ViewBag.CustomersCount = await _context.Customers.CountAsync();
            ViewBag.OrdersCount = await _context.Orders.CountAsync();

            ViewBag.TotalRevenue = await _context.Orders
                .SumAsync(o => (decimal?)o.TotalPrice) ?? 0;

            var topCustomer = await _context.Customers
                .OrderByDescending(c => c.PurchaseCount)
                .FirstOrDefaultAsync();

            ViewBag.TopCustomer = topCustomer?.FullName ?? "Нет данных";

            var lowStockProducts = await _context.Products
                .Where(p => p.StockQuantity <= 3)
                .OrderBy(p => p.StockQuantity)
                .ToListAsync();

            return View(lowStockProducts);
        }
    }
}