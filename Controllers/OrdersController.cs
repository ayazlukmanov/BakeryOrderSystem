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
     int[] ProductIds,
     int[] Quantities,
     string Status,
     string? Comment)
        {
            var userName = HttpContext.Session.GetString("UserName");

            if (userName == null)
                return RedirectToAction("Login", "Account");

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.FullName == userName);

            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var customer = await _context.Customers.FindAsync(CustomerId);

            if (customer == null)
            {
                TempData["Error"] = "Клиент не найден.";
                return RedirectToAction(nameof(Create));
            }

            var selectedItems = ProductIds
                .Zip(Quantities, (productId, quantity) => new { productId, quantity })
                .Where(x => x.productId > 0 && x.quantity > 0)
                .GroupBy(x => x.productId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Quantity = g.Sum(x => x.quantity)
                })
                .ToList();

            if (!selectedItems.Any())
            {
                TempData["Error"] = "Выберите хотя бы один товар.";
                return RedirectToAction(nameof(Create));
            }

            decimal totalPrice = 0;

            var orderProducts = new List<(Product Product, int Quantity)>();

            foreach (var item in selectedItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);

                if (product == null)
                {
                    TempData["Error"] = "Один из товаров не найден.";
                    return RedirectToAction(nameof(Create));
                }

                if (product.StockQuantity < item.Quantity)
                {
                    TempData["Error"] = $"Недостаточно товара на складе: {product.Name}.";
                    return RedirectToAction(nameof(Create));
                }

                totalPrice += product.Price * item.Quantity;
                orderProducts.Add((product, item.Quantity));
            }

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

            foreach (var item in orderProducts)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.Product.Id,
                    Quantity = item.Quantity,
                    Price = item.Product.Price
                });

                item.Product.StockQuantity -= item.Quantity;

                if (item.Product.StockQuantity <= 0)
                {
                    item.Product.StockQuantity = 0;
                    item.Product.IsAvailable = false;
                }
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
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return RedirectToAction(nameof(Index));

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int Id, string Status, string? Comment)
        {
            var order = await _context.Orders.FindAsync(Id);

            if (order == null)
                return RedirectToAction(nameof(Index));

            order.Status = Status;
            order.Comment = Comment ?? "";

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