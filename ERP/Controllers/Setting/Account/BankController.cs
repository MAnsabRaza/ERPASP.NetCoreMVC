using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers.Setting.Account
{
    public class BankController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;
        public BankController(AppDbContext context,INotyfService notyf) { _context = context; _notyf = notyf; }
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
                Skip((page - 1) * pageSize).Take(pageSize).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            var model = new Bank
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            ViewBag.companyList = await _context.Company.ToListAsync();
            ViewBag.Bank = bankList;
            return View("~/Views/Setting/Account/Bank.cshtml", model);
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
                Skip((page - 1) * pageSize).Take(pageSize).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            ViewBag.Bank = bankList;
            ViewBag.companyList = await _context.Company.ToListAsync();
            //return View("Bank", bank);
            return View("~/Views/Setting/Account/Bank.cshtml", bank);
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var bank = await _context.Bank.FindAsync(id);
            if (bank != null)
            {
                _context.Bank.Remove(bank);
                await _context.SaveChangesAsync();
                _notyf.Success("Bank Delete Successfully");
            }
            return RedirectToAction("Bank");
        }
        [HttpPost]
        public async Task<IActionResult> Create(Bank bank)
        {
            try
            {
                var companyIdString = HttpContext.Session.GetString("companyId");
                if (string.IsNullOrEmpty(companyIdString))
                {
                    _notyf.Error("Session expired. Please log in again.");
                    return RedirectToAction("Login", "Auth");
                }
                int companyId=int.Parse(companyIdString);
                bank.companyId=companyId;
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
                        exisitngBank.companyId =companyId;
                        _context.Update(exisitngBank);
                        await _context.SaveChangesAsync();
                        _notyf.Success("Bank Update Successfully");
                    }
                }
                else
                {
                    _context.Bank.Add(bank);
                    await _context.SaveChangesAsync();
                    _notyf.Success("Bank Create Successfully");

                }
                return RedirectToAction("Bank");
            }
            catch (Exception ex)
            {
                _notyf.Error($"An Error occurred: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }
    }
}
