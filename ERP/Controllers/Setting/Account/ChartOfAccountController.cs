using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers.Setting.Account
{
    public class ChartOfAccountController : Controller
    {
        private readonly AppDbContext _contaxt;
        private readonly INotyfService _notyf;
        public ChartOfAccountController(AppDbContext contaxt, INotyfService notyf)
        {
            _notyf = notyf;
            _contaxt = contaxt;
        }

        public async Task<IActionResult> ChartOfAccount(string searchString, int page1 = 1, int pageSize = 5)
        {
            var query = _contaxt.ChartOfAccount.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.name.Contains(searchString));
            }

            var totalItems = await query.CountAsync();

            var level1List = await query
                .Include(c => c.Company)
                .Include(a => a.AccountType)
                .OrderBy(c => c.Id)
                .Skip((page1 - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new ChartOfAccount
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };

            ViewBag.compantList = await _contaxt.Company.ToListAsync();
            ViewBag.accountTypeList = await _contaxt.AccountType.ToListAsync();
            ViewBag.Level1 = level1List;
            ViewBag.SearchString = searchString;
            ViewBag.Page1 = page1;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            return View("~/Views/Setting/Account/ChartOfAccount.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, string searchString, int page1 = 1, int pageSize = 5)
        {
            var chartOfAccount = await _contaxt.ChartOfAccount.FindAsync(id);
            if (chartOfAccount == null) return NotFound();

            var query = _contaxt.ChartOfAccount.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.name.Contains(searchString));
            }

            var totalItems = await query.CountAsync();

            var level1List = await query
                .Include(c => c.Company)
                .Include(a => a.AccountType)
                .OrderBy(c => c.Id)
                .Skip((page1 - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.compantList = await _contaxt.Company.ToListAsync();
            ViewBag.accountTypeList = await _contaxt.AccountType.ToListAsync();
            ViewBag.Level1 = level1List;
            ViewBag.Page1 = page1;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.SearchString = searchString;
            return View("~/Views/Setting/Account/ChartOfAccount.cshtml", chartOfAccount);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var chartOfAccount = await _contaxt.ChartOfAccount.FindAsync(id);
            if (chartOfAccount != null)
            {
                _contaxt.ChartOfAccount.Remove(chartOfAccount);
                await _contaxt.SaveChangesAsync();
                _notyf.Success("Chart Of Account Deleted Successfully");
            }
            return RedirectToAction("ChartOfAccount");
        }

        [HttpPost]
        public async Task<IActionResult> CreateLevel1(ChartOfAccount level1)
        {
            var companyIdString = HttpContext.Session.GetString("companyId");
            if (string.IsNullOrEmpty(companyIdString))
            {
                _notyf.Error("Session expired. Please log in again.");
                return RedirectToAction("Login", "Auth");
            }

            int companyId = int.Parse(companyIdString);
            level1.companyId = companyId;

            if (level1.Id > 0)
            {
                var existing = await _contaxt.ChartOfAccount.FindAsync(level1.Id);
                if (existing != null)
                {
                    existing.current_date = level1.current_date;
                    existing.name = level1.name;
                    existing.accountTypeId = level1.accountTypeId;
                    existing.companyId = companyId;
                    _contaxt.Update(existing);
                    await _contaxt.SaveChangesAsync();
                    _notyf.Success("Level One Updated Successfully");
                }
            }
            else
            {
                _contaxt.ChartOfAccount.Add(level1);
                await _contaxt.SaveChangesAsync();
                _notyf.Success("Level One Created Successfully");
            }
            return RedirectToAction("ChartOfAccount");
        }
    }
}