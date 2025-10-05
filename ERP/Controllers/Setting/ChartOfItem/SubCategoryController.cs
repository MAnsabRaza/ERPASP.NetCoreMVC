using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Printing;

namespace ERP.Controllers.Setting.ChartOfItem
{
    public class SubCategoryController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notfy;
        public SubCategoryController(AppDbContext context,INotyfService notfy)
        {
            _notfy = notfy;
            _context = context;
        }
        public async Task<IActionResult> SubCategory(string searchString, int page = 1, int pageSize = 5)
        {
            var query = _context.SubCategory.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(sb => sb.sub_category_name.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var subCategoryList = await query.
                Include(c => c.Category).
                OrderBy(sb => sb.Id).
                Skip((page - 1) * pageSize).
                Take(pageSize).ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;


            var model = new SubCategory
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            ViewBag.categoryList = await _context.Category
                    .Where(c => c.status == true)
                    .ToListAsync();
            ViewBag.SubCategory = subCategoryList;
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
                _notfy.Success("SubCategory Delete Successfully");
            }
            return RedirectToAction("SubCategory");
        }
        [HttpPost]
        public async Task<IActionResult> Create(SubCategory subcategory)
        {
            try
            {
                if (subcategory.Id > 0)
                {
                    var existingSub = await _context.SubCategory.FindAsync(subcategory.Id);
                    if (existingSub != null)
                    {
                        existingSub.status = subcategory.status;
                        existingSub.current_date = subcategory.current_date;
                        existingSub.sub_category_description = subcategory.sub_category_description;
                        existingSub.sub_category_name = subcategory.sub_category_name;
                        existingSub.categoryId = subcategory.categoryId;
                        _context.Update(existingSub);
                        await _context.SaveChangesAsync();
                        _notfy.Success("SubCategory Update Successfully");
                    }
                }
                else
                {
                    _context.SubCategory.Add(subcategory);
                    await _context.SaveChangesAsync();
                    _notfy.Success("SubCategory Create Successfully");
                }
                return RedirectToAction("SubCategory");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string searchString, int page = 1, int pageSize = 5)
        {
            var sub = await _context.SubCategory.FindAsync(id);
            if (sub == null)
            {
                return NotFound();
            }
            var query = _context.SubCategory.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(sb => sb.sub_category_name.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var subCategoryList = await query.
                Include(c => c.Category).
            OrderBy(sb => sb.Id).
                Skip((page - 1) * pageSize).
                Take(pageSize).ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            ViewBag.categoryList = await _context.Category.
                Where(c => c.status == true).
                ToListAsync();
            ViewBag.SubCategory = subCategoryList;
            return View("~/Views/Setting/ChartOfItem/SubCategory.cshtml", sub);
        }
    }
}
