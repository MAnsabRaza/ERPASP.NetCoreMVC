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
        public async Task<IActionResult> Create(JournalViewModel jvm, string JournalDetailsJson)
        {
            try
            {
                List<JournalDetail> details = new List<JournalDetail>();

                if (!string.IsNullOrEmpty(JournalDetailsJson))
                {
                    details = System.Text.Json.JsonSerializer.Deserialize<List<JournalDetail>>(JournalDetailsJson);
                    jvm.JournalEntry.total_credit = details.Sum(d => d.credit_amount);
                    jvm.JournalEntry.total_debit = details.Sum(d => d.debit_amount);
                }

                if (jvm.JournalEntry.Id > 0) 
                {
                    var existingEntry = await _context.JournalEntry
                        .FirstOrDefaultAsync(x => x.Id == jvm.JournalEntry.Id);

                    if (existingEntry != null)
                    {
                        // Update main entry
                        existingEntry.current_date = jvm.JournalEntry.current_date;
                        existingEntry.due_date = jvm.JournalEntry.due_date;
                        existingEntry.posted_date = jvm.JournalEntry.posted_date;
                        existingEntry.companyId = jvm.JournalEntry.companyId;
                        existingEntry.etype = jvm.JournalEntry.etype;
                        existingEntry.description = jvm.JournalEntry.description;
                        existingEntry.total_credit = jvm.JournalEntry.total_credit;
                        existingEntry.total_debit = jvm.JournalEntry.total_debit;

                        _context.Update(existingEntry);
                        await _context.SaveChangesAsync();

                        // Clear old details
                        var existingDetails = _context.JournalDetail.Where(d => d.journalEntryId == existingEntry.Id);
                        _context.JournalDetail.RemoveRange(existingDetails);
                        await _context.SaveChangesAsync();

                        // Insert new details (if any)
                        if (details.Count > 0)
                        {
                            foreach (var d in details)
                            {
                                d.journalEntryId = existingEntry.Id; // ensure FK
                                _context.JournalDetail.Add(d);
                            }
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                else // Insert
                {
                    _context.JournalEntry.Add(jvm.JournalEntry);
                    await _context.SaveChangesAsync();

                    if (details.Count > 0)
                    {
                        foreach (var d in details)
                        {
                            d.journalEntryId = jvm.JournalEntry.Id; // ensure FK
                            _context.JournalDetail.Add(d);
                        }
                        await _context.SaveChangesAsync();
                    }
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

            // Populate dropdowns again
            ViewBag.CompanyList = await _context.Company.ToListAsync();
            ViewBag.ChartOfAccount = await _context.ChartOfAccount
                .Where(c => c.parentAccountId != null)
                .ToListAsync();

            // For list tab
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

            return View("Journal", model); // ✅ Now passing JournalViewModel
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
            return RedirectToAction("journal");
        }
    }
}
