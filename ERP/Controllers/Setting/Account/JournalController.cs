using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ERP.Controllers.Setting.Account
{
    public class JournalController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;
        public JournalController(AppDbContext context,INotyfService notyf)
        {
            _notyf=notyf;
            _context = context;
        }

        public async Task<IActionResult> Journal(int page = 1, int pageSize = 5, string activeTab = "form")
        {
            var model = new JournalViewModel
            {
                JournalEntry = new JournalEntry
                {
                    current_date = DateOnly.FromDateTime(DateTime.Now),
                    due_date = DateOnly.FromDateTime(DateTime.Now),
                    posted_date = DateOnly.FromDateTime(DateTime.Now)
                },
                JournalDetail = new List<JournalDetail>()
            };

            var totalJournal = await _context.JournalEntry.CountAsync();
            var journalData = await _context.JournalEntry
                .Include(j => j.Company)
                .OrderBy(j => j.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(j => new JournalViewModel
                {
                    JournalEntry = j,
                    JournalDetail = _context.JournalDetail
                        .Include(d => d.ChartOfAccount)
                        .Where(d => d.journalEntryId == j.Id)
                        .ToList()
                })
                .ToListAsync();

            ViewBag.TotalItems = totalJournal;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.ActiveTab = activeTab;
            ViewBag.CompanyList = await _context.Company.ToListAsync();
            ViewBag.ChartOfAccount = await _context.ChartOfAccount
                .Where(c => c.parentAccountId != null)
                .ToListAsync();
            ViewBag.Journal = journalData;

            return View("~/Views/Setting/Account/Journal.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JournalViewModel jvm, int page = 1, int pageSize = 5)
        {
            try
            {
                if (jvm.JournalEntry.Id > 0)
                {
                    var existingEntry = await _context.JournalEntry
                        .FirstOrDefaultAsync(x => x.Id == jvm.JournalEntry.Id);

                    if (existingEntry != null)
                    {
                        existingEntry.current_date = jvm.JournalEntry.current_date;
                        existingEntry.due_date = jvm.JournalEntry.due_date;
                        existingEntry.posted_date = jvm.JournalEntry.posted_date;
                        existingEntry.companyId = jvm.JournalEntry.companyId;
                        existingEntry.etype = jvm.JournalEntry.etype;
                        existingEntry.description = jvm.JournalEntry.description;
                        existingEntry.total_credit = jvm.JournalDetail.Sum(x => x.credit_amount);
                        existingEntry.total_debit = jvm.JournalDetail.Sum(x => x.debit_amount);

                        _context.Update(existingEntry);
                        await _context.SaveChangesAsync();

                        var existingDetails = _context.JournalDetail.Where(d => d.journalEntryId == existingEntry.Id);
                        _context.JournalDetail.RemoveRange(existingDetails);
                        await _context.SaveChangesAsync();

                        foreach (var d in jvm.JournalDetail)
                        {
                            d.journalEntryId = existingEntry.Id;
                            _context.JournalDetail.Add(d);
                        }
                        await _context.SaveChangesAsync();
                        _notyf.Success("Journal Voucher Update Successfully");
                    }
                }
                else
                {
                    jvm.JournalEntry.total_credit = jvm.JournalDetail.Sum(x => x.credit_amount);
                    jvm.JournalEntry.total_debit = jvm.JournalDetail.Sum(x => x.debit_amount);

                    _context.JournalEntry.Add(jvm.JournalEntry);
                    await _context.SaveChangesAsync();

                    foreach (var d in jvm.JournalDetail)
                    {
                        d.journalEntryId = jvm.JournalEntry.Id;
                        _context.JournalDetail.Add(d);
                    }
                    await _context.SaveChangesAsync();
                    _notyf.Success("Journal Voucher Create Successfully");
                }

                return RedirectToAction("Journal", new { page, pageSize, activeTab = "list" });
            }
            catch (Exception ex)
            {
                _notyf.Error($"An Error Occurred: {ex.Message}");
                var inner = ex.InnerException != null ? ex.InnerException.Message : "";
                return BadRequest($"{ex.Message} - {inner}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, int page = 1, int pageSize = 5)
        {
            var journal = await _context.JournalEntry
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (journal == null)
            {
                return NotFound();
            }

            var journalDetails = await _context.JournalDetail
                .Include(d => d.ChartOfAccount)
                .Where(d => d.journalEntryId == id)
                .ToListAsync();

            var model = new JournalViewModel
            {
                JournalEntry = journal,
                JournalDetail = journalDetails
            };

            var totalJournal = await _context.JournalEntry.CountAsync();
            var journalData = await _context.JournalEntry
                .Include(j => j.Company)
                .OrderBy(j => j.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(j => new JournalViewModel
                {
                    JournalEntry = j,
                    JournalDetail = _context.JournalDetail
                        .Include(d => d.ChartOfAccount)
                        .Where(d => d.journalEntryId == j.Id)
                        .ToList()
                })
                .ToListAsync();

            ViewBag.TotalItems = totalJournal;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.ActiveTab = "form";
            ViewBag.CompanyList = await _context.Company.ToListAsync();
            ViewBag.ChartOfAccount = await _context.ChartOfAccount
                .Where(c => c.parentAccountId != null)
                .ToListAsync();
            ViewBag.Journal = journalData;

            return View("~/Views/Setting/Account/Journal.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, int page = 1, int pageSize = 5)
        {
            var journal = await _context.JournalEntry.FindAsync(id);
            if (journal != null)
            {
                var detail = _context.JournalDetail.Where(d => d.journalEntryId == id);
                _context.JournalDetail.RemoveRange(detail);
                _context.JournalEntry.Remove(journal);
                await _context.SaveChangesAsync();
                _notyf.Success("Journal Voucher Delete Successfully");
            }
            return RedirectToAction("Journal", new { page, pageSize, activeTab = "list" });
        }
    }
}