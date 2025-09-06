using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Quic;

namespace ERP.Controllers.Setting.ChartOfItem
{
    public class BrandController : Controller
    {
        private readonly AppDbContext _context;
        public BrandController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Brand(string searchString, int page = 1, int pageSize = 5)
        {
            var query = _context.Brand.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(b => b.brand_name.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var brandList = await query.
                OrderBy(b => b.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize).ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;

            var model = new Brand
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            ViewBag.Brand = brandList;
            //return View("Brand",model);
            return View("~/Views/Setting/ChartOfItem/Brand.cshtml", model);
        }
        [HttpPost]
        public async Task<IActionResult> Create(Brand brand)
        {
            try
            {
                if (brand.Id > 0)
                {
                    var exisitngBrand = await _context.Brand.FindAsync(brand.Id);
                    if (exisitngBrand != null)
                    {
                        exisitngBrand.current_date = brand.current_date;
                        exisitngBrand.status = brand.status;
                        exisitngBrand.brand_name = brand.brand_name;
                        exisitngBrand.brand_description = brand.brand_description;
                        _context.Update(exisitngBrand);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    _context.Brand.Add(brand);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("Brand");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _context.Brand.FindAsync(id);
            if (brand != null)
            {
                _context.Brand.Remove(brand);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Brand");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string searchString, int page = 1, int pageSize = 5)
        {
            var brand = await _context.Brand.FindAsync(id);
            if (brand == null)
            {
                return NotFound();
            }
            var query = _context.Brand.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(b => b.brand_name.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var brandList = await query.
                OrderBy(b => b.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize).ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            ViewBag.Brand = brandList;
            //return View("Brand", brand);
            return View("~/Views/Setting/ChartOfItem/Brand.cshtml", brand);
        }
    }
}
