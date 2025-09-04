using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models.ChartOfItem;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers.Setting.ChartOfItem
{
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;
        public CategoryController(AppDbContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }
        public async Task<IActionResult> Category(string searchString, int page = 1, int pageSize = 5)
        {
            var query = _context.Category.AsQueryable();
            //search
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(u => u.category_name.Contains(searchString));
            }

            //Pagination
            var totalItems = await query.CountAsync();
            var categoryList = await query.
                        OrderBy(u => u.Id).
                        Skip((page - 1) * pageSize).
                        Take(pageSize).
                        ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            var model = new Category
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            ViewBag.Category = categoryList;
            return View("~/Views/Setting/ChartOfItem/Category.cshtml", model);
            //return View("Category",model);
            //return View("~/Views/Setting/ChartOfItem/Category.cshtml", model);
        }
        [HttpPost]
        public async Task<IActionResult> Create(Category category)
        {
            try
            {
                if (category.Id > 0)
                {
                    var exisitngCategory = await _context.Category.FindAsync(category.Id);
                    if (exisitngCategory != null)
                    {
                        exisitngCategory.current_date = category.current_date;
                        exisitngCategory.status = category.status;
                        exisitngCategory.category_name = category.category_name;
                        exisitngCategory.category_description = category.category_description;
                        _context.Update(exisitngCategory);
                        await _context.SaveChangesAsync();
                        _notyf.Success("Category Update successfully!");
                    }
                }
                else
                {
                    _context.Category.Add(category);
                    await _context.SaveChangesAsync();
                    _notyf.Success("Record saved successfully!");
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
        public async Task<IActionResult> Edit(int id, string searchString, int page = 1, int pageSize = 5)
        {
            var category = await _context.Category.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            var query = _context.Category.AsQueryable();
            //search
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(u => u.category_name.Contains(searchString));
            }

            //Pagination
            var totalItems = await query.CountAsync();
            var categoryList = await query.
                        OrderBy(u => u.Id).
                        Skip((page - 1) * pageSize).
                        Take(pageSize).
                        ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            ViewBag.Category = categoryList;
            //return View("Category", category);
            return View("~/Views/Setting/ChartOfItem/Category.cshtml", category);
        }

    }
}
