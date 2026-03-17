using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers.Setting.UserManagement
{
    public class TaxSetupController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;

        public TaxSetupController(AppDbContext context, INotyfService notyf)
        {
            _notyf = notyf;
            _context = context;
        }

        // ✅ GET: List + Form
        public async Task<IActionResult> TaxSetup(string searchString, int page = 1, int pageSize = 5)
        {
            var query = _context.TaxSetup.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(t => t.tax_name.Contains(searchString));
            }

            var totalItems = await query.CountAsync();
            var taxList = await query
                .OrderBy(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            ViewBag.TaxList = taxList;

            var model = new TaxSetup
            {
                applicable_on = "Both",
                status = true
            };

            return View("~/Views/Setting/UserManagement/TaxSetup.cshtml", model);
        }

        // ✅ GET: Edit — loads record into form
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string searchString, int page = 1, int pageSize = 5)
        {
            var tax = await _context.TaxSetup.FindAsync(id);
            if (tax == null)
            {
                _notyf.Error("Tax record not found.");
                return RedirectToAction("TaxSetup");
            }

            var query = _context.TaxSetup.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(t => t.tax_name.Contains(searchString));
            }

            var totalItems = await query.CountAsync();
            var taxList = await query
                .OrderBy(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            ViewBag.TaxList = taxList;

            return View("~/Views/Setting/UserManagement/TaxSetup.cshtml", tax);
        }

        // ✅ POST: Create or Update
        [HttpPost]
        public async Task<IActionResult> Create(TaxSetup taxSetup)
        {
            try
            {
                var companyIdString = HttpContext.Session.GetString("companyId");

                if (string.IsNullOrEmpty(companyIdString))
                {
                    _notyf.Error("Company Required");
                    return RedirectToAction("TaxSetup");
                }

                int companyId = int.Parse(companyIdString);

                if (taxSetup.Id > 0)
                {
                    // ✅ UPDATE
                    var existing = await _context.TaxSetup.FindAsync(taxSetup.Id);
                    if (existing != null)
                    {
                        existing.tax_name = taxSetup.tax_name;
                        existing.percentage = taxSetup.percentage;
                        existing.applicable_on = taxSetup.applicable_on;
                        existing.status = taxSetup.status;
                        existing.companyId = companyId;

                        _context.Update(existing);
                        await _context.SaveChangesAsync();
                        _notyf.Success("Tax updated successfully.");
                    }
                    else
                    {
                        _notyf.Error("Tax record not found.");
                    }
                }
                else
                {
                    // ✅ CREATE
                    taxSetup.companyId = companyId;
                    _context.TaxSetup.Add(taxSetup);
                    await _context.SaveChangesAsync();
                    _notyf.Success("Tax created successfully.");
                }

                return RedirectToAction("TaxSetup");
            }
            catch (Exception ex)
            {
                _notyf.Error($"An error occurred: {ex.Message}");
                return RedirectToAction("TaxSetup");
            }
        }

        // ✅ POST: Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var tax = await _context.TaxSetup.FindAsync(id);
            if (tax != null)
            {
                _context.TaxSetup.Remove(tax);
                await _context.SaveChangesAsync();
                _notyf.Success("Tax deleted successfully.");
            }
            else
            {
                _notyf.Error("Tax record not found.");
            }

            return RedirectToAction("TaxSetup");
        }
    }
}