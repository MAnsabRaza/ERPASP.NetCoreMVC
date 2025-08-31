using ERP.Models.Account;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers.Setting.Account
{
    public class BankController : Controller
    {
        private readonly AppDbContext _context;
        public BankController(AppDbContext context) { _context = context; }
        public async Task<IActionResult> Bank(string searchString, int page = 1, int pageSize = 5)
        {
            var query = _context.Bank.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(at => at.account_no.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var bankList = await query.
                Include(c => c.Company).
                OrderBy(at => at.Id).
                Skip((page - 1) * pageSize).Take(totalItems).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            var model = new AccountType
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            ViewBag.Company = await _context.Company.ToListAsync();
            ViewBag.Bank = bankList;
            return View("Bank", model);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string searchString, int page = 1, int pageSize = 5)
        {
            var bank = await _context.Bank.FindAsync(id);
            if (bank == null)
            {
                return NotFound();
            }
            var query = _context.Bank.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(at => at.account_no.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var bankList = await query.
                Include(c=>c.Company).
                OrderBy(at => at.Id).
                Skip((page - 1) * pageSize).Take(totalItems).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            ViewBag.Bank = bankList;
            ViewBag.Company = await _context.Company.ToListAsync();
            return View("Bank", bank);
        }
        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var bank = await _context.Bank.FindAsync(id);
            if (bank != null)
            {
                _context.Bank.Remove(bank);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Bank");
        }
        [HttpPost]
        public async Task<IActionResult> Create(Bank bank)
        {
            try
            {
                if (bank.Id > 0)
                {
                    var exisitngBank= await _context.Bank.FindAsync(bank.Id);
                    if (exisitngBank != null)
                    {
                        exisitngBank.current_date = bank.current_date;
                        exisitngBank.status = bank.status;
                        exisitngBank.account_no = bank.account_no;
                        exisitngBank.name = bank.name;
                        exisitngBank.bank_name = bank.bank_name;
                        exisitngBank.opening_balance = bank.opening_balance;
                        exisitngBank.companyId = bank.companyId;
                        _context.Update(exisitngBank);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    _context.Bank.Add(bank);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("Bank");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
