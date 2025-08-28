using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers.Setting
{
    public class BrandController : Controller
    {
        private readonly AppDbContext _context;
        public BrandController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Brand()
        {
            var model = new Brand
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            ViewBag.Brand = await _context.Brand.ToListAsync();
            //return View("Brand",model);
            return View("~/Views/Setting/ChartOfItem/Brand.cshtml", model);
        }
        [HttpPost]
        public async Task<IActionResult> Create(Brand brand)
        {
            try
            {
                if(brand.Id > 0)
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
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var brand=await _context.Brand.FindAsync(id);
            if(brand != null)
            {
                _context.Brand.Remove(brand);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Brand");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var brand = await _context.Brand.FindAsync(id);
            if(brand == null)
            {
                return NotFound();
            }
            ViewBag.Brand = await _context.Brand.ToListAsync();
            //return View("Brand", brand);
            return View("~/Views/Setting/ChartOfItem/Brand.cshtml", brand);
        }
    }
}
