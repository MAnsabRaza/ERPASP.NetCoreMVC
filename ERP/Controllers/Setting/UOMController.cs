using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers.Setting
{
    public class UOMController : Controller
    {
        private readonly AppDbContext _context;
        public UOMController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> UOM()
        {
            var model = new UOM
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            ViewBag.UOM = await _context.UOM.ToListAsync();
            return View("~/Views/Setting/ChartOfItem/UOM.cshtml", model);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var uom = await _context.UOM.FindAsync(id);
            if (uom == null)
            {
                return NotFound();
            }
            ViewBag.UOM = await _context.UOM.ToListAsync();
            return View("~/Views/Setting/ChartOfItem/UOM.cshtml", uom);
        }
        [HttpPost]
        public async Task<IActionResult> Create(UOM uom)
        {
            try
            {
                if(uom.Id > 0)
                {
                    var existingUom= await _context.UOM.FindAsync(uom.Id);
                    if(existingUom!=null){
                        existingUom.status = uom.status;
                        existingUom.current_date = uom.current_date;
                        existingUom.uom_name=uom.uom_name;
                        _context.Update(existingUom);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    _context.UOM.Add(uom);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("UOM");
            }
            catch (Exception ex)
            {
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
            }
            return RedirectToAction("UOM");
        }
    }
}
