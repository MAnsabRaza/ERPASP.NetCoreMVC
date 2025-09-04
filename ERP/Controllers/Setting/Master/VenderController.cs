using ERP.Models.Master;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers.Setting.Master
{
    public class VenderController : Controller
    {
        private readonly AppDbContext _context;
        public VenderController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Vender(string searchString, int page = 1, int pageSize = 5)
        {
            var query = _context.Vender.AsQueryable();
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(v => v.name.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var venderList = await query.
                Include(c => c.Company).
                OrderBy(v => v.Id).Skip((page - 1) * pageSize).Take(pageSize).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            var model = new Vender
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            ViewBag.companyList = await _context.Company.
             Where(c => c.status == true).
             ToListAsync();
            ViewBag.Vender = venderList;
            return View("~/Views/Setting/Master/Vender.cshtml", model);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string searchString, int page = 1, int pageSize = 5)
        {
            var vender = await _context.Vender.FindAsync(id);
            if (vender == null)
            {
                return NotFound();
            }
            var query = _context.Vender.AsQueryable();
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(v => v.name.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var venderList = await query.
                Include(c => c.Company).
                OrderBy(v => v.Id).Skip((page - 1) * pageSize).Take(pageSize).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            ViewBag.companyList = await _context.Company.
           Where(c => c.status == true).
           ToListAsync();
            ViewBag.Vender = venderList;
            return View("~/Views/Setting/Master/Vender.cshtml", vender);
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var vender = await _context.Vender.FindAsync(id);
            if (vender != null)
            {
                _context.Vender.Remove(vender);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Vender");
        }
        [HttpPost]
        public async Task<IActionResult> Create(Vender vender)
        {
            try
            {
                if (vender.Id > 0)
                {
                    var existingVender = await _context.Vender.FindAsync(vender.Id);
                    if (existingVender != null)
                    {
                        existingVender.current_date = vender.current_date;
                        existingVender.name = vender.name;
                        existingVender.email = vender.email;
                        existingVender.address = vender.address;
                        existingVender.city = vender.city;
                        existingVender.status = vender.status;
                        existingVender.country = vender.country;
                        existingVender.phone = vender.phone;
                        existingVender.current_balance = vender.current_balance;
                        existingVender.companyId = vender.companyId;
                        _context.Update(existingVender);

                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    _context.Vender.Add(vender);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("Vender");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
