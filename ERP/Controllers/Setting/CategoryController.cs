using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers.Setting
{
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;
        public CategoryController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Category()
        {
            var model = new Category
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            ViewBag.Category = await _context.Category.ToListAsync();
            //return View("Category",model);
            return View("~/Views/Setting/ChartOfItem/Category.cshtml", model);
        }
        [HttpPost]
        public async Task<IActionResult> Create(Category category)
        {
            try
            {
                if (category.Id > 0)
                {
                    var exisitngCategory= await _context.Category.FindAsync(category.Id);
                    if (exisitngCategory != null)
                    {
                        exisitngCategory.current_date = category.current_date;
                        exisitngCategory.status = category.status;
                        exisitngCategory.category_name = category.category_name;
                        exisitngCategory.category_description = category.category_description;
                        _context.Update(exisitngCategory);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    _context.Category.Add(category);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("Category");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Category.FindAsync(id);
            if (category != null)
            {
                _context.Category.Remove(category);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Category");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Category.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            ViewBag.Category = await _context.Category.ToListAsync();
            //return View("Category", category);
            return View("~/Views/Setting/ChartOfItem/Category.cshtml", category);
        }
    
    }
}
