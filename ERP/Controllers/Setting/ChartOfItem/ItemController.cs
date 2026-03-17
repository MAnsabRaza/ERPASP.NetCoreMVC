using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers.Setting.ChartOfItem
{
    public class ItemController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;

        public ItemController(AppDbContext context, INotyfService notyf)
        {
            _notyf = notyf;
            _context = context;
        }

        // ✅ GET: Item List + Form
        public async Task<IActionResult> Item(string searchString, int page = 1, int pageSize = 10, string activeTab = "form")
        {
            var query = _context.Item.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(it => it.item_name.Contains(searchString) || it.item_barcode.Contains(searchString));
            }

            var totalItems = await query.CountAsync();
            var itemList = await query
                .Include(c => c.Category)
                .Include(sc => sc.SubCategory)
                .Include(b => b.Brand)
                .Include(u => u.UOM)
                .OrderByDescending(u => u.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            await LoadViewBags(searchString, page, pageSize, totalItems, itemList);

            var model = new Item
            {
                current_date = DateOnly.FromDateTime(DateTime.Now),
                status = true,
                isExpireable = false
            };

            ViewBag.ActiveTab = activeTab;
            return View("~/Views/Setting/ChartOfItem/Item.cshtml", model);
        }

        // ✅ GET: Edit
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string searchString, int page = 1, int pageSize = 10)
        {
            var item = await _context.Item.FindAsync(id);
            if (item == null)
            {
                _notyf.Error("Item not found.");
                return RedirectToAction("Item");
            }

            var query = _context.Item.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
                query = query.Where(it => it.item_name.Contains(searchString) || it.item_barcode.Contains(searchString));

            var totalItems = await query.CountAsync();
            var itemList = await query
                .Include(c => c.Category)
                .Include(sc => sc.SubCategory)
                .Include(b => b.Brand)
                .Include(u => u.UOM)
                .OrderByDescending(u => u.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            await LoadViewBags(searchString, page, pageSize, totalItems, itemList);
            ViewBag.ActiveTab = "form";

            return View("~/Views/Setting/ChartOfItem/Item.cshtml", item);
        }

        // ✅ POST: Create or Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Item item)
        {
            try
            {
                var companyIdString = HttpContext.Session.GetString("companyId");
                var userIdString = HttpContext.Session.GetString("userId");

                if (string.IsNullOrEmpty(companyIdString) || string.IsNullOrEmpty(userIdString))
                {
                    _notyf.Error("Session expired. Please log in again.");
                    return RedirectToAction("Login", "Auth");
                }

                // ── Auto-calculate total_amount ──
                decimal rate = item.rate;
                int qty = item.qty;
                decimal discount = item.discount_amount ?? 0;
                item.total_amount = (rate * qty) - discount;
                if (item.total_amount < 0) item.total_amount = 0;

                // ── isExpireable false → clear expiry_date ──
                if (item.isExpireable != true)
                {
                    item.expiry_date = null;
                    item.isExpireable = false;
                }

                if (item.Id > 0)
                {
                    // UPDATE
                    var existing = await _context.Item.FindAsync(item.Id);
                    if (existing != null)
                    {
                        existing.current_date = item.current_date;
                        existing.expiry_date = item.expiry_date;
                        existing.isExpireable = item.isExpireable;
                        existing.remark = item.remark;
                        existing.item_name = item.item_name;
                        existing.item_barcode = item.item_barcode;
                        existing.status = item.status;
                        existing.categoryId = item.categoryId;
                        existing.subCategoryId = item.subCategoryId;
                        existing.brandId = item.brandId;
                        existing.uomId = item.uomId;
                        existing.qty = item.qty;
                        existing.rate = item.rate;
                        existing.purchase_rate = item.purchase_rate;
                        existing.sale_rate = item.sale_rate;
                        existing.purchase_dic = item.purchase_dic;
                        existing.sale_dic = item.sale_dic;
                        existing.discount_amount = item.discount_amount;
                        existing.total_amount = item.total_amount;
                        existing.min_stock_level = item.min_stock_level;
                        existing.max_stock_level = item.max_stock_level;

                        _context.Update(existing);
                        await _context.SaveChangesAsync();
                        _notyf.Success("Item updated successfully.");
                    }
                    else
                    {
                        _notyf.Error("Item not found.");
                    }
                }
                else
                {
                    // CREATE
                    _context.Item.Add(item);
                    await _context.SaveChangesAsync();
                    _notyf.Success("Item created successfully.");
                }

                return RedirectToAction("Item", new { activeTab = "list" });
            }
            catch (Exception ex)
            {
                _notyf.Error($"Error: {ex.Message}");
                return RedirectToAction("Item");
            }
        }

        // ✅ POST: Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Item.FindAsync(id);
            if (item != null)
            {
                _context.Item.Remove(item);
                await _context.SaveChangesAsync();
                _notyf.Success("Item deleted successfully.");
            }
            else
            {
                _notyf.Error("Item not found.");
            }

            return RedirectToAction("Item", new { activeTab = "list" });
        }

        // ✅ Private helper — avoid repeating ViewBag assignments
        private async Task LoadViewBags(string searchString, int page, int pageSize, int totalItems, List<Item> itemList)
        {
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            ViewBag.Item = itemList;
            ViewBag.categoryList = await _context.Category.Where(c => c.status == true).OrderBy(c => c.category_name).ToListAsync();
            ViewBag.brandList = await _context.Brand.Where(b => b.status == true).OrderBy(b => b.brand_name).ToListAsync();
            ViewBag.uomList = await _context.UOM.Where(u => u.status == true).OrderBy(u => u.uom_name).ToListAsync();
            ViewBag.subCategoryList = await _context.SubCategory.Where(sb => sb.status == true).OrderBy(sb => sb.sub_category_name).ToListAsync();
        }
    }
}