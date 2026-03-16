using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers.Finance
{
    public class CashReceiptController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;

        public CashReceiptController(AppDbContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }

        // ════════════════════════════════════════════════════════
        // INDEX
        // ════════════════════════════════════════════════════════
        public async Task<IActionResult> CashReceipt(int page = 1, int pageSize = 5, string activeTab = "form")
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

            // ✅ Count only CashReceipt entries
            var totalJournal = await _context.JournalEntry
                .CountAsync(j => j.etype == "CashReceipt");

            var journalData = await _context.JournalEntry
                .Include(j => j.Company)
                .Where(j => j.etype == "CashReceipt")
                .OrderByDescending(j => j.Id)
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

            // ✅ Only child accounts
            ViewBag.ChartOfAccount = await _context.ChartOfAccount.ToListAsync();

            ViewBag.Journal = journalData;

            return View("~/Views/Finance/CashReceipt.cshtml", model);
        }

        // ════════════════════════════════════════════════════════
        // CREATE / UPDATE
        // ════════════════════════════════════════════════════════
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
                    _notyf.Error("Session expired. Please login again.");
                    return RedirectToAction("Login", "Auth");
                }

                int companyId = int.Parse(companyIdString);
                int userId = int.Parse(userIdString);

                jvm.JournalEntry.companyId = companyId;
                jvm.JournalEntry.userId = userId;
                jvm.JournalEntry.etype = "CashReceipt";

                // ✅ Validation
                if (jvm.JournalDetail == null || !jvm.JournalDetail.Any())
                {
                    _notyf.Error("Please add at least one journal entry.");
                    return RedirectToAction("CashReceipt", new { activeTab = "form" });
                }

                decimal totalDebit = jvm.JournalDetail.Sum(x => x.debit_amount);
                decimal totalCredit = jvm.JournalDetail.Sum(x => x.credit_amount);

                if (totalDebit != totalCredit)
                {
                    _notyf.Error($"Entry not balanced! Debit: {totalDebit:N2}, Credit: {totalCredit:N2}");
                    return RedirectToAction("CashReceipt", new { activeTab = "form" });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // ✅ Helper: Get Running Balance
                    async Task<decimal> GetRunningBalance(int chartOfAccountId)
                    {
                        return await _context.Ledger
                            .Where(l => l.chartOfAccountId == chartOfAccountId && l.companyId == companyId)
                            .OrderByDescending(l => l.Id)
                            .Select(l => l.running_balance)
                            .FirstOrDefaultAsync();
                    }

                    // ═════════════════════════════════════════════
                    // UPDATE
                    // ═════════════════════════════════════════════
                    if (jvm.JournalEntry.Id > 0)
                    {
                        var existingEntry = await _context.JournalEntry
                            .FirstOrDefaultAsync(x => x.Id == jvm.JournalEntry.Id);

                        if (existingEntry == null)
                        {
                            _notyf.Error("Journal entry not found.");
                            await transaction.RollbackAsync();
                            return NotFound();
                        }

                        // STEP 1: Update JournalEntry
                        existingEntry.current_date = jvm.JournalEntry.current_date;
                        existingEntry.due_date = jvm.JournalEntry.due_date;
                        existingEntry.posted_date = jvm.JournalEntry.posted_date;
                        existingEntry.description = jvm.JournalEntry.description;
                        existingEntry.total_debit = totalDebit;
                        existingEntry.total_credit = totalCredit;
                        existingEntry.etype = "CashReceipt";
                        _context.Update(existingEntry);

                        // STEP 2: Delete old details & ledger
                        var oldDetails = _context.JournalDetail.Where(d => d.journalEntryId == existingEntry.Id);
                        _context.JournalDetail.RemoveRange(oldDetails);

                        var oldLedger = _context.Ledger.Where(l => l.journalEntryId == existingEntry.Id);
                        _context.Ledger.RemoveRange(oldLedger);

                        await _context.SaveChangesAsync();

                        // STEP 3: Insert new details
                        foreach (var detail in jvm.JournalDetail)
                        {
                            detail.journalEntryId = existingEntry.Id;
                            _context.JournalDetail.Add(detail);
                        }

                        // STEP 4: Insert new ledger
                        foreach (var detail in jvm.JournalDetail)
                        {
                            decimal runningBalance = await GetRunningBalance(detail.chartOfAccountId);
                            var account = await _context.ChartOfAccount
                                .Include(c => c.AccountType)
                                .FirstOrDefaultAsync(c => c.Id == detail.chartOfAccountId);

                            if (account == null) continue;

                            decimal newBalance = runningBalance;
                            string accountTypeName = account.AccountType?.account_name?.ToLower() ?? "";

                            if (accountTypeName == "asset" || accountTypeName == "expense")
                            {
                                newBalance = runningBalance + detail.debit_amount - detail.credit_amount;
                            }
                            else if (accountTypeName == "liability" || accountTypeName == "equity" || accountTypeName == "revenue")
                            {
                                newBalance = runningBalance + detail.credit_amount - detail.debit_amount;
                            }

                            _context.Ledger.Add(new Ledger
                            {
                                current_date = detail.current_date ?? jvm.JournalEntry.current_date,
                                companyId = companyId,
                                chartOfAccountId = detail.chartOfAccountId,
                                journalEntryId = existingEntry.Id,
                                debit_amount = detail.debit_amount,
                                credit_amount = detail.credit_amount,
                                running_balance = newBalance,
                                description = detail.description
                            });
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _notyf.Success("Cash Receipt Updated Successfully");
                    }

                    // ═════════════════════════════════════════════
                    // CREATE NEW
                    // ═════════════════════════════════════════════
                    else
                    {
                        // STEP 1: Insert JournalEntry
                        jvm.JournalEntry.total_debit = totalDebit;
                        jvm.JournalEntry.total_credit = totalCredit;
                        jvm.JournalEntry.etype = "CashReceipt";
                        _context.JournalEntry.Add(jvm.JournalEntry);
                        await _context.SaveChangesAsync();

                        // STEP 2: Insert JournalDetails
                        foreach (var detail in jvm.JournalDetail)
                        {
                            detail.journalEntryId = jvm.JournalEntry.Id;
                            _context.JournalDetail.Add(detail);
                        }

                        // STEP 3: Insert Ledger with running balance
                        foreach (var detail in jvm.JournalDetail)
                        {
                            decimal runningBalance = await GetRunningBalance(detail.chartOfAccountId);
                            var account = await _context.ChartOfAccount
                                .Include(c => c.AccountType)
                                .FirstOrDefaultAsync(c => c.Id == detail.chartOfAccountId);

                            if (account == null) continue;

                            decimal newBalance = runningBalance;
                            string accountTypeName = account.AccountType?.account_name?.ToLower() ?? "";

                            if (accountTypeName == "asset" || accountTypeName == "expense")
                            {
                                newBalance = runningBalance + detail.debit_amount - detail.credit_amount;
                            }
                            else if (accountTypeName == "liability" || accountTypeName == "equity" || accountTypeName == "revenue")
                            {
                                newBalance = runningBalance + detail.credit_amount - detail.debit_amount;
                            }

                            _context.Ledger.Add(new Ledger
                            {
                                current_date = detail.current_date ?? jvm.JournalEntry.current_date,
                                companyId = companyId,
                                chartOfAccountId = detail.chartOfAccountId,
                                journalEntryId = jvm.JournalEntry.Id,
                                debit_amount = detail.debit_amount,
                                credit_amount = detail.credit_amount,
                                running_balance = newBalance,
                                description = detail.description
                            });
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _notyf.Success("Cash Receipt Created Successfully");
                    }

                    return RedirectToAction("CashReceipt", new { page, pageSize, activeTab = "list" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Error: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                _notyf.Error($"Error: {ex.Message}");
                return BadRequest($"{ex.Message} - {ex.InnerException?.Message}");
            }
        }

        // ════════════════════════════════════════════════════════
        // EDIT
        // ════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Edit(int id, int page = 1, int pageSize = 5)
        {
            var journal = await _context.JournalEntry
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (journal == null)
            {
                _notyf.Error("Journal entry not found.");
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

            var totalJournal = await _context.JournalEntry.CountAsync(j => j.etype == "CashReceipt");
            var journalData = await _context.JournalEntry
                .Include(j => j.Company)
                .Where(j => j.etype == "CashReceipt")
                .OrderByDescending(j => j.Id)
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
            ViewBag.ChartOfAccount = await _context.ChartOfAccount.ToListAsync();
            ViewBag.Journal = journalData;

            return View("~/Views/Finance/CashReceipt.cshtml", model);
        }

        // ════════════════════════════════════════════════════════
        // DELETE
        // ════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int page = 1, int pageSize = 5)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var journal = await _context.JournalEntry.FindAsync(id);
                if (journal != null)
                {
                    // STEP 1: Delete Ledger
                    var ledgerEntries = _context.Ledger.Where(l => l.journalEntryId == id);
                    _context.Ledger.RemoveRange(ledgerEntries);

                    // STEP 2: Delete JournalDetail
                    var details = _context.JournalDetail.Where(d => d.journalEntryId == id);
                    _context.JournalDetail.RemoveRange(details);

                    // STEP 3: Delete JournalEntry
                    _context.JournalEntry.Remove(journal);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    _notyf.Success("Cash Receipt Deleted Successfully");
                }

                return RedirectToAction("CashReceipt", new { page, pageSize, activeTab = "list" });
            }
            catch (Exception ex)
            {
                _notyf.Error($"Error: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }
    }
}