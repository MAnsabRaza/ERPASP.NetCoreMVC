using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.Text.Json.Nodes;

namespace ERP.Controllers.Setting.Account
{
    public class JournalController : Controller
    {
        private readonly AppDbContext _context;
        public JournalController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Journal()
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

            var journalData = await _context.JournalEntry
                .Include(j => j.Company)
                .Select(j => new JournalViewModel
                {
                    JournalEntry = j,
                    JournalDetail = _context.JournalDetail
                        .Include(d => d.ChartOfAccount)
                        .Where(d => d.journalEntryId == j.Id)
                        .ToList()
                })
                .ToListAsync();

            ViewBag.CompanyList = await _context.Company.ToListAsync();
            ViewBag.ChartOfAccount = await _context.ChartOfAccount
                .Where(c => c.parentAccountId != null)
                .ToListAsync();
            ViewBag.Journal = journalData;

            return View("Journal", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JournalViewModel jvm)
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
                }

                return RedirectToAction("Journal");
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : "";
                return BadRequest($"{ex.Message} - {inner}");
            }
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
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

            ViewBag.CompanyList = await _context.Company.ToListAsync();
            ViewBag.ChartOfAccount = await _context.ChartOfAccount
                .Where(c => c.parentAccountId != null)
                .ToListAsync();

            var journalData = await _context.JournalEntry
                .Include(j => j.Company)
                .Select(j => new JournalViewModel
                {
                    JournalEntry = j,
                    JournalDetail = _context.JournalDetail
                        .Include(d => d.ChartOfAccount)
                        .Where(d => d.journalEntryId == j.Id)
                        .ToList()
                })
                .ToListAsync();

            ViewBag.Journal = journalData;

            return View("Journal", model);
        }


        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var journal = await _context.JournalEntry.FindAsync(id);
            if (journal != null)
            {
                var detail = _context.JournalDetail.Where(d => d.journalEntryId == id);
                _context.JournalDetail.RemoveRange(detail);
                _context.JournalEntry.Remove(journal);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Journal");
        }
    }
}