using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers.Finance
{
    public class AccountReceivableController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;

        public AccountReceivableController(AppDbContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }

        // ════════════════════════════════════════════════════════
        // INDEX
        // ════════════════════════════════════════════════════════
        public async Task<IActionResult> AccountReceivable(int page = 1, int pageSize = 5, string activeTab = "form")
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

            var totalJournal = await _context.JournalEntry
                .CountAsync(j => j.etype == "AccountReceivable");

            var journalData = await _context.JournalEntry
                .Include(j => j.Company)
                .Where(j => j.etype == "AccountReceivable")
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
            ViewBag.Customers = await _context.Customer.ToListAsync();
            ViewBag.ChartOfAccount = await _context.ChartOfAccount.ToListAsync();
            ViewBag.Journal = journalData;

            return View("~/Views/Finance/AccountReceivable.cshtml", model);
        }

        // ════════════════════════════════════════════════════════
        // CREATE / UPDATE
        // ════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JournalViewModel jvm)
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
                jvm.JournalEntry.etype = "AccountReceivable";

                if (jvm.JournalDetail == null || !jvm.JournalDetail.Any())
                {
                    _notyf.Error("Please add journal details.");
                    return RedirectToAction("AccountReceivable", new { activeTab = "form" });
                }

                decimal totalDebit = jvm.JournalDetail.Sum(x => x.debit_amount);
                decimal totalCredit = jvm.JournalDetail.Sum(x => x.credit_amount);

                if (totalDebit != totalCredit)
                {
                    _notyf.Error("Debit and Credit must be equal.");
                    return RedirectToAction("AccountReceivable", new { activeTab = "form" });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // ═════════════════════════════════════════════
                    // UPDATE EXISTING ENTRY
                    // ═════════════════════════════════════════════
                    if (jvm.JournalEntry.Id > 0)
                    {
                        var existingEntry = await _context.JournalEntry
                            .FirstOrDefaultAsync(x => x.Id == jvm.JournalEntry.Id);

                        if (existingEntry == null)
                        {
                            _notyf.Error("Journal entry not found.");
                            return RedirectToAction("AccountReceivable");
                        }

                        // 1️⃣ Reverse Old Customer Balance
                        if (existingEntry.customerId.HasValue)
                        {
                            var oldCustomer = await _context.Customer
                                .FindAsync(existingEntry.customerId.Value);
                            if (oldCustomer != null)
                            {
                                oldCustomer.current_balance += existingEntry.total_credit;
                                _context.Customer.Update(oldCustomer);
                            }
                        }

                        // 2️⃣ Delete Old JournalDetails
                        var oldDetails = _context.JournalDetail
                            .Where(d => d.journalEntryId == existingEntry.Id);
                        _context.JournalDetail.RemoveRange(oldDetails);

                        // 3️⃣ Delete Old Ledger rows
                        var oldLedger = _context.Ledger
                            .Where(l => l.journalEntryId == existingEntry.Id);
                        _context.Ledger.RemoveRange(oldLedger);

                        await _context.SaveChangesAsync();

                        // 4️⃣ Update JournalEntry
                        existingEntry.current_date = jvm.JournalEntry.current_date;
                        existingEntry.due_date = jvm.JournalEntry.due_date;
                        existingEntry.posted_date = jvm.JournalEntry.posted_date;
                        existingEntry.description = jvm.JournalEntry.description;
                        existingEntry.total_debit = totalDebit;
                        existingEntry.total_credit = totalCredit;
                        existingEntry.customerId = jvm.JournalEntry.customerId;
                        _context.JournalEntry.Update(existingEntry);
                        await _context.SaveChangesAsync();

                        // 5️⃣ Insert New JournalDetails
                        foreach (var detail in jvm.JournalDetail)
                        {
                            detail.journalEntryId = existingEntry.Id;
                            _context.JournalDetail.Add(detail);
                        }
                        await _context.SaveChangesAsync();

                        // 6️⃣ Insert New Ledger rows
                        await InsertLedgerEntries(
                            jvm.JournalDetail,
                            existingEntry.Id,
                            companyId,
                            jvm.JournalEntry.current_date);
                        await _context.SaveChangesAsync();

                        // 7️⃣ Apply New Customer Balance
                        if (jvm.JournalEntry.customerId.HasValue)
                        {
                            var newCustomer = await _context.Customer
                                .FindAsync(jvm.JournalEntry.customerId.Value);
                            if (newCustomer != null)
                            {
                                newCustomer.current_balance -= totalCredit;
                                _context.Customer.Update(newCustomer);
                            }
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        _notyf.Success("Account Receivable Updated Successfully");
                    }

                    // ═════════════════════════════════════════════
                    // CREATE NEW ENTRY
                    // ═════════════════════════════════════════════
                    else
                    {
                        // 1️⃣ Save JournalEntry
                        jvm.JournalEntry.total_debit = totalDebit;
                        jvm.JournalEntry.total_credit = totalCredit;
                        _context.JournalEntry.Add(jvm.JournalEntry);
                        await _context.SaveChangesAsync();

                        // 2️⃣ Save JournalDetails
                        foreach (var detail in jvm.JournalDetail)
                        {
                            detail.journalEntryId = jvm.JournalEntry.Id;
                            _context.JournalDetail.Add(detail);
                        }
                        await _context.SaveChangesAsync();

                        // 3️⃣ Insert Ledger rows
                        await InsertLedgerEntries(
                            jvm.JournalDetail,
                            jvm.JournalEntry.Id,
                            companyId,
                            jvm.JournalEntry.current_date);
                        await _context.SaveChangesAsync();

                        // 4️⃣ Update Customer Balance
                        if (jvm.JournalEntry.customerId.HasValue)
                        {
                            var customer = await _context.Customer
                                .FindAsync(jvm.JournalEntry.customerId.Value);
                            if (customer != null)
                            {
                                customer.current_balance -= totalCredit;
                                _context.Customer.Update(customer);
                            }
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        _notyf.Success("Account Receivable Created Successfully");
                    }

                    return RedirectToAction("AccountReceivable", new { activeTab = "list" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _notyf.Error(ex.Message);
                    return RedirectToAction("AccountReceivable", new { activeTab = "form" });
                }
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("AccountReceivable", new { activeTab = "form" });
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

            var totalJournal = await _context.JournalEntry
                .CountAsync(j => j.etype == "AccountReceivable");

            var journalData = await _context.JournalEntry
                .Include(j => j.Company)
                .Where(j => j.etype == "AccountReceivable")
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
            ViewBag.Customers = await _context.Customer.ToListAsync();
            ViewBag.ChartOfAccount = await _context.ChartOfAccount.ToListAsync();
            ViewBag.Journal = journalData;

            return View("~/Views/Finance/AccountReceivable.cshtml", model);
        }

        // ════════════════════════════════════════════════════════
        // DELETE
        // ════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var journal = await _context.JournalEntry
                    .FirstOrDefaultAsync(j => j.Id == id);

                if (journal == null)
                {
                    _notyf.Error("Record not found.");
                    return RedirectToAction("AccountReceivable");
                }

                // ✅ STEP 1: Reverse Customer Balance
                if (journal.customerId.HasValue)
                {
                    var customer = await _context.Customer
                        .FindAsync(journal.customerId.Value);
                    if (customer != null)
                    {
                        customer.current_balance += journal.total_credit;
                        _context.Customer.Update(customer);
                        await _context.SaveChangesAsync();
                    }
                }

                // ✅ STEP 2: Delete Ledger
                var ledgerEntries = _context.Ledger
                    .Where(l => l.journalEntryId == id);
                _context.Ledger.RemoveRange(ledgerEntries);

                // ✅ STEP 3: Delete JournalDetail
                var details = _context.JournalDetail
                    .Where(d => d.journalEntryId == id);
                _context.JournalDetail.RemoveRange(details);

                // ✅ STEP 4: Delete JournalEntry
                _context.JournalEntry.Remove(journal);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _notyf.Success("Account Receivable Voucher Deleted Successfully");

                return RedirectToAction("AccountReceivable", new { activeTab = "list" });
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("AccountReceivable");
            }
        }

        // ════════════════════════════════════════════════════════
        // PRIVATE HELPER — Ledger Insert
        // ════════════════════════════════════════════════════════

        /// <summary>
        /// Har JournalDetail ke liye ek Ledger row insert karta hai.
        /// Running Balance formula (Asset accounts):
        ///   new_balance = previous_balance + debit - credit
        /// </summary>
        private async Task InsertLedgerEntries(
            List<JournalDetail> details,
            int journalEntryId,
            int companyId,
            DateOnly entryDate)
        {
            foreach (var detail in details)
            {
                // Is chart of account ka last running balance lo
                var lastLedger = await _context.Ledger
                    .Where(l => l.chartOfAccountId == detail.chartOfAccountId
                             && l.companyId == companyId)
                    .OrderByDescending(l => l.Id)
                    .FirstOrDefaultAsync();

                decimal previousBalance = lastLedger?.running_balance ?? 0;

                // Asset account formula: +Debit -Credit
                decimal newBalance = previousBalance + detail.debit_amount - detail.credit_amount;

                var ledger = new Ledger
                {
                    current_date = entryDate,
                    companyId = companyId,
                    chartOfAccountId = detail.chartOfAccountId,
                    journalEntryId = journalEntryId,
                    debit_amount = detail.debit_amount,
                    credit_amount = detail.credit_amount,
                    running_balance = newBalance,
                    description = detail.description
                };

                _context.Ledger.Add(ledger);
            }
        }
    }
}