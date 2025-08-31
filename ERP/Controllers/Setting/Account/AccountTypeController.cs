using ERP.Models;
using ERP.Models.Account;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers.Setting.Account
{
    public class AccountTypeController : Controller
    {
        private readonly AppDbContext _context;
        public AccountTypeController(AppDbContext context) { _context = context; }
        public async Task<IActionResult> AccountType(string searchString,int page = 1,int pageSize=5)
        {
            var query = _context.AccountType.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(at => at.account_name.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var accountTypeList = await query.
                OrderBy(at=>at.Id).
                Skip((page-1)*pageSize).Take(totalItems).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            var model = new AccountType
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            ViewBag.AccountType = accountTypeList;
            return View("AccountType",model);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string searchString, int page = 1, int pageSize = 5)
        {
            var accountType = await _context.AccountType.FindAsync(id);
            if(accountType == null)
            {
                return NotFound();
            }
            var query = _context.AccountType.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(at => at.account_name.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var accountTypeList = await query.
                OrderBy(at => at.Id).
                Skip((page - 1) * pageSize).Take(totalItems).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            ViewBag.AccountType = accountTypeList;
            return View("AccountType", accountType);
        }
        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var accountType = await _context.AccountType.FindAsync(id);
            if(accountType != null)
            {
                 _context.AccountType.Remove(accountType);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("AccountType");
        }
        [HttpPost]
        public async Task<IActionResult> Create(AccountType accountType)
        {
            try
            {
                if (accountType.Id > 0)
                {
                    var exisitngAccount = await _context.AccountType.FindAsync(accountType.Id);
                    if(exisitngAccount != null)
                    {
                        exisitngAccount.current_date=accountType.current_date;
                        exisitngAccount.status = accountType.status;
                        exisitngAccount.account_name = accountType.account_name;
                        _context.Update(accountType);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    _context.AccountType.Add(accountType);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("AccountType");
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
