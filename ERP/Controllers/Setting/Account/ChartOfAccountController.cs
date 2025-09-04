using ERP.Models.Account;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers.Setting.Account
{
    public class ChartOfAccountController : Controller
    {
        private readonly AppDbContext _contaxt;
        public ChartOfAccountController(AppDbContext contaxt)
        {
            _contaxt = contaxt;
        }

        public async Task<IActionResult> ChartOfAccount(string searchString, int page1 = 1, int page2 = 1, int pageSize = 5)
        {
            var query = _contaxt.ChartOfAccount.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.name.Contains(searchString));
            }

            var totalItemsLevel1 = await query.Where(c => c.parentAccountId == null).CountAsync();
            var totalItemsLevel2 = await query.Where(c => c.parentAccountId != null).CountAsync();

            var level1List = await query.Include(c => c.Company)
                                        .Include(a => a.AccountType)
                                        .Where(c => c.parentAccountId == null)
                                        .OrderBy(c => c.Id)
                                        .Skip((page1 - 1) * pageSize)
                                        .Take(pageSize)
                                        .ToListAsync();

            var level2List = await query.Include(c => c.Company)
                                        .Include(a => a.AccountType)
                                        .Where(c => c.parentAccountId != null)
                                        .OrderBy(c => c.Id)
                                        .Skip((page2 - 1) * pageSize)
                                        .Take(pageSize)
                                        .ToListAsync();

            var model = new ChartOfAccount
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            ViewBag.compantList = await _contaxt.Company.ToListAsync();
            ViewBag.accountTypeList = await _contaxt.AccountType.ToListAsync();
            ViewBag.parentAccount = await _contaxt.ChartOfAccount.Where(c => c.parentAccountId == null).ToListAsync();
            ViewBag.Level1 = level1List;
            ViewBag.Level2 = level2List;
            ViewBag.SearchString = searchString;
            ViewBag.Page1 = page1;
            ViewBag.Page2 = page2;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItemsLevel1 = totalItemsLevel1;
            ViewBag.TotalItemsLevel2 = totalItemsLevel2;

            return View("ChartOfAccount", model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, string searchString, int page1 = 1, int page2 = 1, int pageSize = 5)
        {
            var chartOfAccount = await _contaxt.ChartOfAccount.FindAsync(id);
            if (chartOfAccount == null) return NotFound();

            var query = _contaxt.ChartOfAccount.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.name.Contains(searchString));
            }

            // Totals
            var totalItemsLevel1 = await query.Where(c => c.parentAccountId == null).CountAsync();
            var totalItemsLevel2 = await query.Where(c => c.parentAccountId != null).CountAsync();

            // Lists
            var level1List = await query.Include(c => c.Company).Include(a => a.AccountType)
                                        .Where(c => c.parentAccountId == null)
                                        .OrderBy(c => c.Id)
                                        .Skip((page1 - 1) * pageSize)
                                        .Take(pageSize)
                                        .ToListAsync();

            var level2List = await query.Include(c => c.Company).Include(a => a.AccountType)
                                        .Where(c => c.parentAccountId != null)
                                        .OrderBy(c => c.Id)
                                        .Skip((page2 - 1) * pageSize)
                                        .Take(pageSize)
                                        .ToListAsync();

            // Dropdowns
            ViewBag.compantList = await _contaxt.Company.ToListAsync();
            ViewBag.accountTypeList = await _contaxt.AccountType.ToListAsync();
            ViewBag.parentAccount = await _contaxt.ChartOfAccount.Where(c => c.parentAccountId == null).ToListAsync();

            // ViewBag for pagination
            ViewBag.Level1 = level1List;
            ViewBag.Level2 = level2List;
            ViewBag.Page1 = page1;
            ViewBag.Page2 = page2;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItemsLevel1 = totalItemsLevel1;
            ViewBag.TotalItemsLevel2 = totalItemsLevel2;
            ViewBag.SearchString = searchString;

            return View("ChartOfAccount", chartOfAccount);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var chartOfAccount = await _contaxt.ChartOfAccount.FindAsync(id);
            if (chartOfAccount != null)
            {
                _contaxt.ChartOfAccount.Remove(chartOfAccount);
                await _contaxt.SaveChangesAsync();
            }
            return RedirectToAction("ChartOfAccount");
        }

        [HttpPost]
        public async Task<IActionResult> CreateLevel1(ChartOfAccount level1)
        {
            if (level1.Id > 0)
            {
                var existing = await _contaxt.ChartOfAccount.FindAsync(level1.Id);
                if (existing != null)
                {
                    existing.current_date = level1.current_date;
                    existing.name = level1.name;
                    existing.accountTypeId = level1.accountTypeId;
                    existing.companyId = level1.companyId;
                    _contaxt.Update(existing);
                }
            }
            else
            {
                _contaxt.ChartOfAccount.Add(level1);
            }
            await _contaxt.SaveChangesAsync();
            return RedirectToAction("ChartOfAccount");
        }

        [HttpPost]
        public async Task<IActionResult> CreateLevel2(ChartOfAccount level2)
        {
            if (level2.Id > 0)
            {
                var existing = await _contaxt.ChartOfAccount.FindAsync(level2.Id);
                if (existing != null)
                {
                    existing.current_date = level2.current_date;
                    existing.name = level2.name;
                    existing.parentAccountId = level2.parentAccountId;
                    existing.accountTypeId = level2.accountTypeId;
                    existing.companyId = level2.companyId;
                    _contaxt.Update(existing);
                }
            }
            else
            {
                _contaxt.ChartOfAccount.Add(level2);
            }
            await _contaxt.SaveChangesAsync();
            return RedirectToAction("ChartOfAccount");
        }
    }
}
