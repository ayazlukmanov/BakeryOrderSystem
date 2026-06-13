using BakeryOrderSystem.Data;
using BakeryOrderSystem.Helpers;
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
                return RedirectToAction("Login", "Account");

            var orders = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                orders = orders.Where(o =>
                    o.Customer.FullName.Contains(search) ||
                    o.User.FullName.Contains(search) ||
                    o.Status.Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
                orders = orders.Where(o => o.Status == status);

            if (!string.IsNullOrEmpty(employee))
                orders = orders.Where(o => o.User.FullName == employee);

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
            ViewBag.Employee = employee;

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

            return View(await orders.ToListAsync());
        }

        public async Task<IActionResult> Create()
        {
            var userName = HttpContext.Session.GetString("UserName");

            if (userName == null)
                return RedirectToAction("Login", "Account");

            ViewBag.Customers = new SelectList(
                await _context.Customers.OrderBy(c => c.FullName).ToListAsync(),
                "Id",
                "FullName"
            );

            ViewBag.Products = await _context.Products
                .Where(p => p.IsAvailable && p.StockQuantity > 0)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            int CustomerId,
            int ProductId,
            int Quantity,
            string Status,
            string? Comment)
        {
            var userName = HttpContext.Session.GetString("UserName");

            if (userName == null)
                return RedirectToAction("Login", "Account");

            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.FullName == userName);

            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var product = await _context.Products.FindAsync(ProductId);

            if (product == null)
            {
                TempData["Error"] = "Выбранный товар не найден.";
                return RedirectToAction(nameof(Create));
            }

            if (Quantity <= 0)
            {
                TempData["Error"] = "Количество товара должно быть больше нуля.";
                return RedirectToAction(nameof(Create));
            }

            if (product.StockQuantity < Quantity)
            {
                TempData["Error"] = "Недостаточно товара на складе.";
                return RedirectToAction(nameof(Create));
            }

            var customer = await _context.Customers.FindAsync(CustomerId);

            if (customer == null)
            {
                TempData["Error"] = "Клиент не найден.";
                return RedirectToAction(nameof(Create));
            }

            var totalPrice = product.Price * Quantity;

            if (customer.DiscountPercent > 0)
            {
                totalPrice -= totalPrice * customer.DiscountPercent / 100;
            }

            var order = new Order
            {
                CustomerId = customer.Id,
                UserId = currentUser.Id,
                Status = Status,
                TotalPrice = totalPrice,
                Comment = Comment ?? "",
                OrderDate = DateTimeHelper.KazanNow()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var orderItem = new OrderItem
            {
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = Quantity,
                Price = product.Price
            };

            _context.OrderItems.Add(orderItem);

            product.StockQuantity -= Quantity;

            if (product.StockQuantity <= 0)
            {
                product.StockQuantity = 0;
                product.IsAvailable = false;
            }

            customer.PurchaseCount++;

            if (customer.PurchaseCount >= 20)
                customer.DiscountPercent = 10;
            else if (customer.PurchaseCount >= 10)
                customer.DiscountPercent = 5;
            else if (customer.PurchaseCount >= 5)
                customer.DiscountPercent = 3;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
                return RedirectToAction(nameof(Index));

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
            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return RedirectToAction(nameof(Index));

            var orderItems = await _context.OrderItems
                .Where(oi => oi.OrderId == id)
                .ToListAsync();

            foreach (var item in orderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);

                if (product != null)
                {
                    product.StockQuantity += item.Quantity;

                    if (product.StockQuantity > 0)
                    {
                        product.IsAvailable = true;
                    }
                }
            }

            if (order.Customer != null)
            {
                if (order.Customer.PurchaseCount > 0)
                {
                    order.Customer.PurchaseCount--;
                }

                if (order.Customer.PurchaseCount >= 20)
                    order.Customer.DiscountPercent = 10;
                else if (order.Customer.PurchaseCount >= 10)
                    order.Customer.DiscountPercent = 5;
                else if (order.Customer.PurchaseCount >= 5)
                    order.Customer.DiscountPercent = 3;
                else
                    order.Customer.DiscountPercent = 0;
            }

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