using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers.Finance
{
    public class JVController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;
        public JVController(AppDbContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }
        public async Task<IActionResult> JV(int page = 1, int pageSize = 5, string activeTab = "form")
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
                 .Where(j => j.etype == "JV")
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
            return View("JV", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JournalViewModel jvm, int page = 1, int pageSize = 5)
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
                jvm.JournalEntry.companyId = companyId;
                jvm.JournalEntry.userId = userId;
                if (jvm.JournalEntry.Id > 0)
                {
                    var existingEntry = await _context.JournalEntry
                        .FirstOrDefaultAsync(x => x.Id == jvm.JournalEntry.Id);

                    if (existingEntry != null)
                    {
                        existingEntry.current_date = jvm.JournalEntry.current_date;
                        existingEntry.due_date = jvm.JournalEntry.due_date;
                        existingEntry.posted_date = jvm.JournalEntry.posted_date;
                        existingEntry.companyId = companyId;
                        existingEntry.userId = userId;
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
                        _notyf.Success("Journal Voucher Voucher Update Successfully");
                    }
                }
                else
                {
                    jvm.JournalEntry.total_credit = jvm.JournalDetail.Sum(x => x.credit_amount);
                    jvm.JournalEntry.total_debit = jvm.JournalDetail.Sum(x => x.debit_amount);
                    jvm.JournalEntry.etype = "JV";
                    _context.JournalEntry.Add(jvm.JournalEntry);
                    await _context.SaveChangesAsync();

                    foreach (var d in jvm.JournalDetail)
                    {
                        d.journalEntryId = jvm.JournalEntry.Id;
                        _context.JournalDetail.Add(d);
                    }
                    await _context.SaveChangesAsync();
                    _notyf.Success("Journal Voucher Voucher Create Successfully");
                }

                return RedirectToAction("JV", new { page, pageSize, activeTab = "list" });
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

            return View("JV", model);
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
                _notyf.Success("Journal Voucher Voucher Delete Successfully");
            }
            return RedirectToAction("JV", new { page, pageSize, activeTab = "list" });
        }
    }
}
