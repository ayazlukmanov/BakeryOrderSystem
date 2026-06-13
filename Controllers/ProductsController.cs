using BakeryOrderSystem.Data;
using BakeryOrderSystem.Helpers;
using BakeryOrderSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BakeryOrderSystem.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search, string category, string sortOrder)
        {
            var userName = HttpContext.Session.GetString("UserName");

            if (userName == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var products = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                products = products.Where(p =>
                    p.Name.Contains(search) ||
                    p.Category.Contains(search));
            }

            if (!string.IsNullOrEmpty(category))
            {
                products = products.Where(p => p.Category == category);
            }

            products = sortOrder switch
            {
                "name_desc" => products.OrderByDescending(p => p.Name),
                "price_asc" => products.OrderBy(p => p.Price),
                "price_desc" => products.OrderByDescending(p => p.Price),
                _ => products.OrderBy(p => p.Name)
            };

            ViewBag.Search = search;
            ViewBag.Category = category;
            ViewBag.SortOrder = sortOrder;

            ViewBag.Categories = await _context.Products
                .Select(p => p.Category)
                .Where(c => c != null && c != "")
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return View(await products.ToListAsync());
        }

        public async Task<IActionResult> Create()
        {
            var role = HttpContext.Session.GetString("Role");

            if (!RoleCheck.IsAdminOrManager(role))
            {
                TempData["Error"] = "У вас нет прав для доступа к данному разделу.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Products = await _context.Products.ToListAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product)
        {
            var role = HttpContext.Session.GetString("Role");

            if (!RoleCheck.IsAdminOrManager(role))
            {
                TempData["Error"] = "У вас нет прав для доступа к данному разделу.";
                return RedirectToAction("Index", "Home");
            }

            product.Description ??= "";
            product.Category ??= "";

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name == product.Category);

            if (category == null)
            {
                category = new Category
                {
                    Name = product.Category
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
            }

            product.CategoryId = category.Id;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var role = HttpContext.Session.GetString("Role");

            if (!RoleCheck.IsAdminOrManager(role))
            {
                TempData["Error"] = "У вас нет прав для доступа к данному разделу.";
                return RedirectToAction("Index", "Home");
            }

            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Products = await _context.Products.ToListAsync();

            return View(product);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(Product product)
        {
            var role = HttpContext.Session.GetString("Role");

            if (!RoleCheck.IsAdminOrManager(role))
            {
                TempData["Error"] = "У вас нет прав для доступа к данному разделу.";
                return RedirectToAction("Index", "Home");
            }

            product.Description ??= "";
            product.Category ??= "";

            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == product.Id);

            if (existingProduct == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name == product.Category);

            if (category == null)
            {
                category = new Category
                {
                    Name = product.Category
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
            }

            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.Category = product.Category;
            existingProduct.CategoryId = category.Id;
            existingProduct.IsAvailable = product.IsAvailable;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var role = HttpContext.Session.GetString("Role");

            if (!RoleCheck.IsAdminOrManager(role))
            {
                TempData["Error"] = "У вас нет прав для доступа к данному разделу.";
                return RedirectToAction("Index", "Home");
            }

            var productInOrder = await _context.OrderItems
                .AnyAsync(oi => oi.ProductId == id);

            if (productInOrder)
            {
                TempData["Error"] = "Нельзя удалить товар, так как он уже используется в заказах.";
                return RedirectToAction(nameof(Index));
            }

            var product = await _context.Products.FindAsync(id);

            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}