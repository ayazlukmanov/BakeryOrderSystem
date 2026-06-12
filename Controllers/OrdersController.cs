using BakeryOrderSystem.Data;
using BakeryOrderSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BakeryOrderSystem.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(string search, string status, string employee, string sortOrder)
        {
            var userName = HttpContext.Session.GetString("UserName");

            if (userName == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                orders = orders.Where(o =>
                    o.Customer.FullName.Contains(search) ||
                    o.User.FullName.Contains(search) ||
                    o.Status.Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
            {
                orders = orders.Where(o => o.Status == status);
            }

            if (!string.IsNullOrEmpty(employee))
            {
                orders = orders.Where(o => o.User.FullName == employee);
            }

            orders = sortOrder switch
            {
                "date_asc" => orders.OrderBy(o => o.OrderDate),
                "price_asc" => orders.OrderBy(o => o.TotalPrice),
                "price_desc" => orders.OrderByDescending(o => o.TotalPrice),
                _ => orders.OrderByDescending(o => o.OrderDate)
            };

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.SortOrder = sortOrder;

            ViewBag.Statuses = await _context.Orders
                .Select(o => o.Status)
                .Where(s => s != null && s != "")
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            ViewBag.Employees = await _context.Users
    .Select(u => u.FullName)
    .Distinct()
    .OrderBy(u => u)
    .ToListAsync();

            ViewBag.Employee = employee;

            return View(await orders.ToListAsync());
        }

        public IActionResult Create()
        {
            ViewBag.Customers = new SelectList(_context.Customers, "Id", "FullName");
            ViewBag.Users = new SelectList(_context.Users, "Id", "FullName");

            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(
            int CustomerId,
            int UserId,
            string Status,
            decimal TotalPrice,
            string? Comment)
        {
            var order = new Order
            {
                CustomerId = CustomerId,
                UserId = UserId,
                Status = Status,
                TotalPrice = TotalPrice,
                Comment = Comment ?? "",
                OrderDate = DateTime.Now
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Customers = new SelectList(_context.Customers, "Id", "FullName");
            ViewBag.Users = new SelectList(_context.Users, "Id", "FullName");

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Order order)
        {
            order.Comment ??= "";

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var orderItems = await _context.OrderItems
                .Where(oi => oi.OrderId == id)
                .ToListAsync();

            if (orderItems.Any())
            {
                _context.OrderItems.RemoveRange(orderItems);
            }

            _context.Orders.Remove(order);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}