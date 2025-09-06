using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers.Setting.Master
{
    public class TransporterController : Controller
    {
        private readonly AppDbContext _context;
        public TransporterController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Transporter(string searchString, int page = 1, int pageSize = 5)
        {
            var query = _context.Transporter.AsQueryable();

            // 🔍 Search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(u => u.name.Contains(searchString));
            }

            // 📄 Pagination
            var totalItems = await query.CountAsync();
            var transporterList = await query
                                .OrderBy(u => u.Id)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToListAsync();

            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;

            var model = new Transporter
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };

            ViewBag.Transporter = transporterList;
            //return View("Transporter", model);
            return View("~/Views/Setting/Master/Transporter.cshtml", model);
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id, string searchString, int page = 1, int pageSize = 5)
        {
            var transporter = await _context.Transporter.FindAsync(id);
            if (transporter == null)
            {
                return NotFound();
            }

            // 👇 same as in UOM action
            var query = _context.Transporter.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(u => u.name.Contains(searchString));
            }

            var totalItems = await query.CountAsync();
            var tranporterList = await query
                                .OrderBy(u => u.Id)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToListAsync();

            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            ViewBag.Transporter = tranporterList;
            //return View("Transporter", transporter);
            return View("~/Views/Setting/Master/Transporter.cshtml", transporter);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Transporter transporter)
        {
            try
            {
                if (transporter.Id > 0)
                {
                    var existingTransporter = await _context.Transporter.FindAsync(transporter.Id);
                    if (existingTransporter != null)
                    {
                        existingTransporter.status = transporter.status;
                        existingTransporter.current_date = transporter.current_date;
                        existingTransporter.name = transporter.name;
                        existingTransporter.transporter_no = transporter.transporter_no;
                        existingTransporter.phone = transporter.phone;
                        existingTransporter.address = transporter.address;
                        existingTransporter.description = transporter.description;

                        _context.Update(existingTransporter);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    _context.Transporter.Add(transporter);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("Transporter");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var transporter = await _context.Transporter.FindAsync(id);
            if (transporter != null)
            {
                _context.Transporter.Remove(transporter);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Transporter");
        }
    }
}
