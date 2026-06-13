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
            var role = HttpContext.Session.GetString("Role");

            if (role != "Администратор" && role != "Менеджер")
            {
                TempData["Error"] = "У вас нет доступа к отчетам.";
                return RedirectToAction("Index", "Home");
            }

            var ordersCount = await _context.Orders.CountAsync();
            var totalRevenue = await _context.Orders.SumAsync(o => (decimal?)o.TotalPrice) ?? 0;

            ViewBag.ProductsCount = await _context.Products.CountAsync();
            ViewBag.CustomersCount = await _context.Customers.CountAsync();
            ViewBag.OrdersCount = ordersCount;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.AverageCheck = ordersCount > 0 ? totalRevenue / ordersCount : 0;

            var topCustomer = await _context.Customers
                .OrderByDescending(c => c.PurchaseCount)
                .FirstOrDefaultAsync();

            ViewBag.TopCustomer = topCustomer?.FullName ?? "Нет данных";
            ViewBag.TopCustomerPurchases = topCustomer?.PurchaseCount ?? 0;
            ViewBag.TopCustomerDiscount = topCustomer?.DiscountPercent ?? 0;

            var topProduct = await _context.OrderItems
                .Include(oi => oi.Product)
                .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
                .Select(g => new
                {
                    Name = g.Key.Name,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.Quantity)
                .FirstOrDefaultAsync();

            ViewBag.TopProduct = topProduct?.Name ?? "Нет данных";
            ViewBag.TopProductQuantity = topProduct?.Quantity ?? 0;

            var topEmployee = await _context.Orders
                .Include(o => o.User)
                .GroupBy(o => new { o.UserId, o.User.FullName })
                .Select(g => new
                {
                    FullName = g.Key.FullName,
                    OrdersCount = g.Count()
                })
                .OrderByDescending(x => x.OrdersCount)
                .FirstOrDefaultAsync();

            ViewBag.TopEmployee = topEmployee?.FullName ?? "Нет данных";
            ViewBag.TopEmployeeOrders = topEmployee?.OrdersCount ?? 0;

            var lowStockProducts = await _context.Products
                .Where(p => p.StockQuantity <= 3)
                .OrderBy(p => p.StockQuantity)
                .ToListAsync();

            return View(lowStockProducts);
        }
    }
}