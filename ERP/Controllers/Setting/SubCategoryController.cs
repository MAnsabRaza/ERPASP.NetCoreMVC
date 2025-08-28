using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers.Setting
{
    public class SubCategoryController : Controller
    {
        private readonly AppDbContext _context;
        public SubCategoryController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> SubCategory()
        {
            var model = new SubCategory
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            ViewBag.categoryList = await _context.Category.ToListAsync();
            ViewBag.SubCategory = await _context.SubCategory.
                Include(c=>c.Category).
                ToListAsync();
            //return View("SubCategory",model);
            return View("~/Views/Setting/ChartOfItem/SubCategory.cshtml", model);
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var subCategory = await _context.SubCategory.FindAsync(id);
            if (subCategory != null)
            {
                 _context.SubCategory.Remove(subCategory);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("SubCategory");
        }
        [HttpPost]
        public async Task<IActionResult> Create(SubCategory subcategory)
        {
            try
            {
                if(subcategory.Id > 0)
                {
                    var existingSub= await _context.SubCategory.FindAsync(subcategory.Id);
                    if(existingSub != null)
                    {
                        existingSub.status = subcategory.status;
                        existingSub.current_date = subcategory.current_date;
                        existingSub.sub_category_description = subcategory.sub_category_description;
                        existingSub.sub_category_name = subcategory.sub_category_name;
                        existingSub.categoryId=subcategory.categoryId;
                        _context.Update(existingSub);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    _context.SubCategory.Add(subcategory);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("SubCategory");
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var sub = await _context.SubCategory.FindAsync(id);
            if(sub == null)
            {
                return NotFound();
            }
            ViewBag.categoryList = await _context.Category.ToListAsync();
            ViewBag.SubCategory = await _context.SubCategory.ToListAsync();
            //return View("SubCategory", sub);

            return View("~/Views/Setting/ChartOfItem/SubCategory.cshtml", sub);
        }
    }
}
