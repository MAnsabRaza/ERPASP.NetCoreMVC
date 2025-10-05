using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers.Setting.ChartOfItem
{
    public class UOMController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;
        public UOMController(AppDbContext context,INotyfService notyf)
        {
            _notyf = notyf;
            _context = context;
        }
        public async Task<IActionResult> UOM(string searchString, int page = 1, int pageSize = 5)
        {
            var query = _context.UOM.AsQueryable();

            // 🔍 Search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(u => u.uom_name.Contains(searchString));
            }

            // 📄 Pagination
            var totalItems = await query.CountAsync();
            var uomList = await query
                                .OrderBy(u => u.Id)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToListAsync();

            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;

            var model = new UOM
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };

            ViewBag.UOM = uomList;
            return View("~/Views/Setting/ChartOfItem/UOM.cshtml", model);
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id, string searchString, int page = 1, int pageSize = 5)
        {
            var uom = await _context.UOM.FindAsync(id);
            if (uom == null)
            {
                return NotFound();
            }

            // 👇 same as in UOM action
            var query = _context.UOM.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(u => u.uom_name.Contains(searchString));
            }

            var totalItems = await query.CountAsync();
            var uomList = await query
                                .OrderBy(u => u.Id)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToListAsync();

            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            ViewBag.UOM = uomList;

            return View("~/Views/Setting/ChartOfItem/UOM.cshtml", uom);
        }

        [HttpPost]
        public async Task<IActionResult> Create(UOM uom)
        {
            try
            {
                if (uom.Id > 0)
                {
                    var existingUom = await _context.UOM.FindAsync(uom.Id);
                    if (existingUom != null)
                    {
                        existingUom.status = uom.status;
                        existingUom.current_date = uom.current_date;
                        existingUom.uom_name = uom.uom_name;
                        _context.Update(existingUom);
                        await _context.SaveChangesAsync();
                        _notyf.Success("UOM Update Successfully");
                    }
                }
                else
                {
                    _context.UOM.Add(uom);
                    await _context.SaveChangesAsync();
                    _notyf.Success("UOM Create Successfully");
                }
                return RedirectToAction("UOM");
            }
            catch (Exception ex)
            {
                _notyf.Error($"An error occurred: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var uom = await _context.UOM.FindAsync(id);
            if (uom != null)
            {
                _context.UOM.Remove(uom);
                await _context.SaveChangesAsync();
                _notyf.Success("UOM Delete Successfully");
            }
            return RedirectToAction("UOM");
        }
    }
}
