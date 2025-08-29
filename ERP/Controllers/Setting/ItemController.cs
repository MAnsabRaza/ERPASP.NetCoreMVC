using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace ERP.Controllers.Setting
{
    public class ItemController : Controller
    {
        private readonly AppDbContext _context;
        public ItemController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Item()
        {
            var model = new Item
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            ViewBag.Item = await _context.Item.
                Include(c => c.Category).
                Include(sc => sc.SubCategory).
                Include(b => b.Brand).
                Include(u => u.UOM).
                ToListAsync();
            ViewBag.categoryList=await _context.Category.ToListAsync();
            ViewBag.brandList=await _context.Brand.ToListAsync();
            ViewBag.uomList=await _context.UOM.ToListAsync();
            ViewBag.subCategoryList=await _context.SubCategory.ToListAsync();
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
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.Item.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            ViewBag.item = await _context.Item.ToListAsync();
            ViewBag.categoryList = await _context.Category.ToListAsync();
            ViewBag.brandList = await _context.Brand.ToListAsync();
            ViewBag.uomList = await _context.UOM.ToListAsync();
            ViewBag.subCategoryList = await _context.SubCategory.ToListAsync();
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
