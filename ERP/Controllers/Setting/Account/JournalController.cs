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
                JournalDetail = new JournalDetail
                {
                    current_date = DateOnly.FromDateTime(DateTime.Now),
                }
            };
            var journalEntries = await _context.JournalEntry
                  .Include(j => j.Company)
                  .ToListAsync();
            var journalDetails = await _context.JournalDetail
                .Include(d => d.ChartOfAccount)
                .ToListAsync();
            var journalData = (from je in journalEntries
                               join jd in journalDetails on je.Id equals jd.journalEntryId
                               select new
                               {
                                   journalEntries=je,
                                   journalDetails=jd
                               }).ToList();
            ViewBag.CompanyList = await _context.Company.ToListAsync();
            ViewBag.ChartOfAccount = await _context.ChartOfAccount.ToListAsync();
            ViewBag.Journal = journalData;
            return View("Journal", model);
        }
        [HttpPost]
        public async Task<IActionResult> Create(JournalViewModel jvm,string JournalDetailsJson)
        {
            try
            {
                if(jvm.JournalEntry.Id > 0)
                {
                    var existingEntry = await _context.JournalEntry
                        .FirstOrDefaultAsync(x => x.Id == jvm.JournalEntry.Id);
                    if (existingEntry != null)
                    {
                        existingEntry.current_date = jvm.JournalEntry.current_date;
                        existingEntry.due_date = jvm.JournalEntry.due_date;
                        existingEntry.posted_date = jvm.JournalEntry.posted_date;
                        existingEntry.companyId=jvm.JournalEntry.companyId;
                        existingEntry.etype = jvm.JournalEntry.etype;
                        existingEntry.description = jvm.JournalEntry.description;
                        existingEntry.total_credit=jvm.JournalEntry.total_credit;
                        existingEntry.total_debit = jvm.JournalEntry.total_debit;
                        _context.Update(existingEntry);
                        await _context.SaveChangesAsync();
                        var existingDetail = _context.JournalDetail.Where(d => d.journalEntryId
                        == existingEntry.Id);
                        _context.JournalDetail.RemoveRange(existingDetail);
                        await _context.SaveChangesAsync();
                        if (!string.IsNullOrEmpty(JournalDetailsJson))
                        {
                            var details = System.Text.Json.JsonSerializer.Deserialize<List<JournalDetail>>(JournalDetailsJson);
                            foreach(var d in details)
                            {
                                d.journalEntryId = existingEntry.Id;
                                _context.JournalDetail.Add(d);
                            }
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                else
                {
                    _context.JournalEntry.Add(jvm.JournalEntry);
                    await _context.SaveChangesAsync();
                    if (!string.IsNullOrEmpty(JournalDetailsJson))
                    {
                        var details = System.Text.Json.JsonSerializer.Deserialize<List<JournalDetail>>(JournalDetailsJson);
                        foreach(var d in details)
                        {
                            d.journalEntryId = jvm.JournalEntry.Id;
                            _context.JournalDetail.Add(d);
                        }
                        await _context.SaveChangesAsync();
                    }
                }
                return RedirectToAction("Journal");
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var journal = await _context.JournalEntry.FindAsync(id);
            if(journal == null)
            {
                return NotFound();
            }
        var journalEntries = await _context.JournalEntry
             .Include(j => j.Company)
             .ToListAsync();
        var journalDetails = await _context.JournalDetail
            .Include(d => d.ChartOfAccount)
            .ToListAsync();
        var journalData = (from je in journalEntries
                           join jd in journalDetails on je.Id equals jd.journalEntryId
                           select new
                           {
                               journalEntries = je,
                               journalDetails = jd
                           }).ToList();
        ViewBag.CompanyList = await _context.Company.ToListAsync();
        ViewBag.ChartOfAccount = await _context.ChartOfAccount.ToListAsync();
        ViewBag.Journal = journalData;

            return View("Journal", journal);
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
