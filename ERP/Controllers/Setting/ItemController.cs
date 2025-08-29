using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.Drawing.Printing;

namespace ERP.Controllers.Setting
{
    public class ItemController : Controller
    {
        private readonly AppDbContext _context;
        public ItemController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Item(string searchString,int page=1,int pageSize=5)
        {
            var query=_context.Item.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query=query.Where(it=>it.item_name.Contains(searchString));
            }
            var totalItems=await query.CountAsync();
            var itemList=await query.
                    Include(c => c.Category).Include(sc => sc.SubCategory).Include(b => b.Brand).Include(u => u.UOM).
                OrderBy(u=>u.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            var model = new Item
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            ViewBag.Item = itemList;
            ViewBag.categoryList = await _context.Category.Where(c => c.status == true).ToListAsync();
            ViewBag.brandList = await _context.Brand.Where(b => b.status == true).ToListAsync();
            ViewBag.uomList = await _context.UOM.Where(u => u.status == true).ToListAsync();
            ViewBag.subCategoryList = await _context.SubCategory.Where(sb => sb.status == true).ToListAsync();
            //return View("Item",model);
            return View("~/Views/Setting/ChartOfItem/Item.cshtml", model);
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Item.FindAsync(id);
            if (item != null)
            {
                _context.Item.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Item");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string searchString, int page = 1,int pageSize=5)
        {
            var item = await _context.Item.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            var query = _context.Item.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(it => it.item_name.Contains(searchString));
            }
            var totalItems = await query.CountAsync();

            var itemList = await query.
                     Include(c => c.Category).Include(sc => sc.SubCategory).Include(b => b.Brand).Include(u => u.UOM).
                 OrderBy(u => u.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;

            ViewBag.item = itemList;
            ViewBag.categoryList = await _context.Category.Where(c => c.status == true).ToListAsync();
            ViewBag.brandList = await _context.Brand.Where(b => b.status == true).ToListAsync();
            ViewBag.uomList = await _context.UOM.Where(u => u.status == true).ToListAsync();
            ViewBag.subCategoryList = await _context.SubCategory.Where(sb => sb.status == true).ToListAsync();
            //return View("Item", item);
            return View("~/Views/Setting/ChartOfItem/Item.cshtml", item);
        }
        [HttpPost]
        public async Task<IActionResult> Create(Item item)
        {
            try
            {
                if(item.Id > 0)
                {
                    var existingItem= await _context.Item.FindAsync(item.Id);
                    if(existingItem != null)
                    {
                        existingItem.current_date = item.current_date;
                        existingItem.item_barcode = item.item_barcode;
                        existingItem.item_name = item.item_name;
                        existingItem.status = item.status;
                        existingItem.categoryId = item.categoryId;
                        existingItem.subCategoryId=item.subCategoryId;
                        existingItem.brandId = item.brandId;
                        existingItem.uomId = item.uomId;
                        existingItem.qty = item.qty;
                        existingItem.purchase_rate = item.purchase_rate;
                        existingItem.sale_rate = item.sale_rate;
                        existingItem.rate = item.rate;
                        existingItem.discount_amount = item.discount_amount;
                        existingItem.total_amount = item.total_amount;
                        existingItem.description = item.description;
                        existingItem.remark = item.remark;
                        _context.Update(existingItem);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    _context.Item.Add(item);
                    await _context.SaveChangesAsync();

                }
                return RedirectToAction("Item");
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
