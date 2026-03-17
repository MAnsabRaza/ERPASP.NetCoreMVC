using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers.Setting.UserManagement
{
    public class FinancialYearController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;

        public FinancialYearController(AppDbContext context, INotyfService notyf)
        {
            _notyf = notyf;
            _context = context;
        }

        public async Task<IActionResult> FinancialYear(string searchString, int page = 1, int pageSize = 5)
        {
            // ✅ Auto-close: end_date <= today → status = "Closed"
            var today = DateOnly.FromDateTime(DateTime.Now);
            var expiredYears = await _context.FinancialYear
                .Where(f => f.end_date <= today && f.status == "Open")
                .ToListAsync();

            if (expiredYears.Any())
            {
                foreach (var fy in expiredYears)
                {
                    fy.status = "Closed";
                }
                await _context.SaveChangesAsync();
            }

            // ✅ Search + Pagination
            var query = _context.FinancialYear.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.year_name.Contains(searchString));
            }

            var totalItems = await query.CountAsync();
            var financialYearList = await query
                .OrderBy(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            ViewBag.FinancialYear = financialYearList;

            var model = new FinancialYear
            {
                current_date = today,
                start_date=today,
                end_date = today,
                status = "Open"
            };

            return View("~/Views/Setting/UserManagement/FinancialYear.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, string searchString, int page = 1, int pageSize = 5)
        {
            var financialYear = await _context.FinancialYear.FindAsync(id);
            if (financialYear == null)
            {
                _notyf.Error("Financial Year not found.");
                return RedirectToAction("FinancialYear");
            }

            var query = _context.FinancialYear.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.year_name.Contains(searchString));
            }

            var totalItems = await query.CountAsync();
            var financialYearList = await query
                .OrderBy(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            ViewBag.FinancialYear = financialYearList;

            return View("~/Views/Setting/UserManagement/FinancialYear.cshtml", financialYear);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var financialYear = await _context.FinancialYear.FindAsync(id);
            if (financialYear != null)
            {
                _context.FinancialYear.Remove(financialYear);
                await _context.SaveChangesAsync();
                _notyf.Success("Financial Year deleted successfully.");
            }
            else
            {
                _notyf.Error("Financial Year not found.");
            }

            return RedirectToAction("FinancialYear");
        }

        [HttpPost]
        public async Task<IActionResult> Create(FinancialYear financialYear)
        {
            try
            {
                var companyIdString = HttpContext.Session.GetString("companyId");
                var userIdString = HttpContext.Session.GetString("userId");

                if (string.IsNullOrEmpty(companyIdString) || string.IsNullOrEmpty(userIdString))
                {
                    _notyf.Error("Session expired. Please log in again.");
                    return RedirectToAction("Login", "Auth");
                }

                int companyId = int.Parse(companyIdString);
                int userId = int.Parse(userIdString);

                // ✅ Auto-close check on save
                var today = DateOnly.FromDateTime(DateTime.Now);
                if (financialYear.end_date <= today)
                {
                    financialYear.status = "Closed";
                }

                if (financialYear.Id > 0)
                {
                    var existing = await _context.FinancialYear.FindAsync(financialYear.Id);
                    if (existing != null)
                    {
                        existing.current_date = financialYear.current_date;
                        existing.year_name = financialYear.year_name;
                        existing.start_date = financialYear.start_date;
                        existing.end_date = financialYear.end_date;
                        existing.companyId = companyId;
                        existing.userId = userId;

                        // ✅ Auto-close on update too
                        existing.status = existing.end_date <= today ? "Closed" : financialYear.status;

                        _context.Update(existing);
                        await _context.SaveChangesAsync();
                        _notyf.Success("Financial Year updated successfully.");
                    }
                }
                else
                {
                    financialYear.companyId = companyId;
                    financialYear.userId = userId;

                    _context.FinancialYear.Add(financialYear);
                    await _context.SaveChangesAsync();
                    _notyf.Success("Financial Year created successfully.");
                }

                return RedirectToAction("FinancialYear");
            }
            catch (Exception ex)
            {
                _notyf.Error($"An error occurred: {ex.Message}");
                return RedirectToAction("FinancialYear");
            }
        }
    }
}