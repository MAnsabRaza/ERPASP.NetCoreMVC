using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers.Purchase
{
    public class PurchaseVoucherController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;

        public PurchaseVoucherController(AppDbContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }

        // ════════════════════════════════════════════════════════════════
        // PRIVATE HELPER: ViewBag data load (dropdowns + list)
        // ════════════════════════════════════════════════════════════════
        private async Task LoadViewBagData(int page, int pageSize)
        {
            ViewBag.Warehouses = await _context.Warehouse.ToListAsync();
            ViewBag.Items = await _context.Item.ToListAsync();
            ViewBag.Venders = await _context.Vender.ToListAsync();
            ViewBag.Transporters = await _context.Transporter.ToListAsync();
            ViewBag.TaxSetup = await _context.TaxSetup.Where(t => t.status == true
            && (t.applicable_on == "Both" || t.applicable_on == "Purchase")).ToListAsync();

            int totalPurchase = await _context.StockMaster
                .CountAsync(d => d.etype == "Purchase");

            var purchaseData = await _context.StockMaster
                .Where(j => j.etype == "Purchase")
                .OrderByDescending(j => j.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(j => new PurchaseListDto
                {
                    Id = j.Id,
                    CurrentDate = j.current_date,
                    Etype = j.etype,
                    Remarks = j.remarks,
                    TotalAmount = j.total_amount,
                    NetAmount = j.net_amount,
                    VenderName = j.Vender != null ? j.Vender.name : null,
                    TransporterNo = j.Transporter != null ? j.Transporter.transporter_no : null
                })
                .ToListAsync();

            ViewBag.TotalItems = totalPurchase;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Purchase = purchaseData;
        }

        // ════════════════════════════════════════════════════════════════
        // PRIVATE HELPER: Running balance for ledger
        // ════════════════════════════════════════════════════════════════
        private async Task<decimal> GetRunningBalance(int chartOfAccountId, int companyId)
        {
            return await _context.Ledger
                .Where(l => l.chartOfAccountId == chartOfAccountId
                         && l.companyId == companyId)
                .OrderByDescending(l => l.Id)
                .Select(l => l.running_balance)
                .FirstOrDefaultAsync();
        }

        // ════════════════════════════════════════════════════════════════
        // GET: Purchase Voucher (new form + list)
        // ════════════════════════════════════════════════════════════════
        public async Task<IActionResult> PurchaseVoucher(
            int page = 1, int pageSize = 5, string activeTab = "form")
        {
            // Auto-increment voucher number
            int nextVoucherNo = (await _context.StockMaster
                .Where(s => s.etype == "Purchase")
                .MaxAsync(s => (int?)s.voucher_no) ?? 0) + 1;

            var model = new PurchaseViewModel
            {
                StockMaster = new StockMaster
                {
                    voucher_no = nextVoucherNo,
                    current_date = DateOnly.FromDateTime(DateTime.Now),
                    due_date = DateOnly.FromDateTime(DateTime.Now),
                    posted_date = DateOnly.FromDateTime(DateTime.Now)
                },
                StockDetail = new List<StockDetail>()
            };

            await LoadViewBagData(page, pageSize);
            ViewBag.ActiveTab = activeTab;

            return View("~/Views/Purchase/PurchaseVoucher.cshtml", model);
        }

        // ════════════════════════════════════════════════════════════════
        // GET: Edit Purchase Voucher
        // ════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Edit(int id, int page = 1, int pageSize = 5)
        {
            var purchase = await _context.StockMaster
                .Include(u => u.User)
                .Include(v => v.Vender)
                .Include(t => t.Transporter)
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (purchase == null) return NotFound();

            var purchaseDetail = await _context.StockDetail
                .Include(it => it.Item)
                .Include(w => w.Warehouse)
                .Where(d => d.StockMasterId == id)
                .ToListAsync();

            var model = new PurchaseViewModel
            {
                StockMaster = purchase,
                StockDetail = purchaseDetail
            };

            await LoadViewBagData(page, pageSize);
            ViewBag.ActiveTab = "form"; // Edit pe hamesha form tab

            return View("~/Views/Purchase/PurchaseVoucher.cshtml", model);
        }

        // ════════════════════════════════════════════════════════════════
        // GET: Purchase Rate for selected item (AJAX)
        // ════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetPurchaseRate(int itemId)
        {
            try
            {
                var item = await _context.Item.FindAsync(itemId);
                return item != null
                    ? Json(new { purchaseRate = item.purchase_rate })
                    : Json(new { purchaseRate = (decimal?)null });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ════════════════════════════════════════════════════════════════
        // GET: Item Modal (partial)
        // ════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> ItemModal()
        {
            var model = new Item { current_date = DateOnly.FromDateTime(DateTime.Now) };
            ViewBag.categoryList = await _context.Category.Where(c => c.status == true).ToListAsync();
            ViewBag.brandList = await _context.Brand.Where(b => b.status == true).ToListAsync();
            ViewBag.uomList = await _context.UOM.Where(u => u.status == true).ToListAsync();
            ViewBag.subCategoryList = await _context.SubCategory.Where(sb => sb.status == true).ToListAsync();
            return PartialView("~/Views/Shared/_ItemModal.cshtml", model);
        }

        // ════════════════════════════════════════════════════════════════
        // POST: Create / Update Purchase Voucher
        // ════════════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> Create(PurchaseViewModel pvm)
        {
            try
            {
                // ── Session validation ──
                var companyIdStr = HttpContext.Session.GetString("companyId");
                var userIdStr = HttpContext.Session.GetString("userId");

                if (string.IsNullOrEmpty(companyIdStr) || string.IsNullOrEmpty(userIdStr))
                {
                    _notyf.Error("Session expired. Please log in again.");
                    return RedirectToAction("Login", "Auth");
                }

                int companyId = int.Parse(companyIdStr);
                int userId = int.Parse(userIdStr);

                pvm.StockMaster.companyId = companyId;
                pvm.StockMaster.userId = userId;

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // ── Chart of Accounts ──
                    var purchaseAccount = await _context.ChartOfAccount
                        .FirstOrDefaultAsync(c => c.name == "Purchase Account");
                    var accountsPayableAccount = await _context.ChartOfAccount
                        .FirstOrDefaultAsync(c => c.name == "Accounts Payable");

                    if (purchaseAccount == null || accountsPayableAccount == null)
                    {
                        await transaction.RollbackAsync();
                        _notyf.Error("Chart of Accounts not found. Please add 'Purchase Account' and 'Accounts Payable'.");
                        return RedirectToAction("PurchaseVoucher");
                    }

                    int purchaseAccountId = purchaseAccount.Id;
                    int accountsPayableId = accountsPayableAccount.Id;

                    // ── Fiscal Year Validation ──
                    // status string hai: "open" / "closed"
                    // Agar OPEN fiscal year exist nahi karti → entry BLOCK karo
                    var currentFiscalYear = await _context.FinancialYear
                        .Where(fy => fy.companyId == companyId
                                  && fy.status.ToLower() == "open")
                        .OrderByDescending(fy => fy.Id)
                        .FirstOrDefaultAsync();

                    if (currentFiscalYear == null)
                    {
                        await transaction.RollbackAsync();
                        _notyf.Error("No open Fiscal Year found. Please open a Fiscal Year before creating a Purchase Voucher.");
                        return RedirectToAction("PurchaseVoucher");
                    }

                    // Ab yahan se fiscalYearId guaranteed non-null hai
                    int fiscalYearId = currentFiscalYear.Id;

                    // ── StockMaster.current_date Fiscal Year Range Check ──
                    if (pvm.StockMaster.current_date < currentFiscalYear.start_date ||
                        pvm.StockMaster.current_date > currentFiscalYear.end_date)
                    {
                        await transaction.RollbackAsync();
                        _notyf.Error($"Entry date {pvm.StockMaster.current_date:dd-MMM-yyyy} is outside the open Fiscal Year range " +
                                     $"({currentFiscalYear.start_date:dd-MMM-yyyy} to {currentFiscalYear.end_date:dd-MMM-yyyy}).");
                        return RedirectToAction("PurchaseVoucher");
                    }

                    // ════════════════════════════════════
                    // A) CREATE NEW PURCHASE  (Id == 0)
                    // ════════════════════════════════════
                    if (pvm.StockMaster.Id == 0)
                    {
                        // STEP 1: StockMaster insert
                        pvm.StockMaster.customerId = null;
                        pvm.StockMaster.etype = "Purchase";
                        pvm.StockMaster.payment_status = "Unpaid";
                        pvm.StockMaster.fiscalYearId = fiscalYearId; // Guaranteed non-null

                        _context.StockMaster.Add(pvm.StockMaster);
                        await _context.SaveChangesAsync();

                        // ── StockMaster.Id validate karo ──
                        if (pvm.StockMaster.Id <= 0)
                        {
                            await transaction.RollbackAsync();
                            _notyf.Error("Failed to generate Purchase Voucher ID. Please try again.");
                            return RedirectToAction("PurchaseVoucher");
                        }

                        // STEP 2: StockDetail insert + Item qty/rate update
                        foreach (var detail in pvm.StockDetail)
                        {
                            detail.StockMasterId = pvm.StockMaster.Id;
                            _context.StockDetail.Add(detail);

                            var item = await _context.Item
                                .FirstOrDefaultAsync(i => i.Id == detail.itemId);
                            if (item != null)
                            {
                                item.qty += detail.qty;
                                item.purchase_rate = detail.rate;
                                item.rate = detail.rate;
                                _context.Update(item);
                            }
                        }

                        // STEP 3: Vendor balance update (liability badhi → balance MINUS)
                        var vendor = await _context.Vender
                            .FirstOrDefaultAsync(v => v.Id == pvm.StockMaster.venderId);
                        if (vendor != null)
                        {
                            vendor.current_balance -= pvm.StockMaster.net_amount;
                            _context.Update(vendor);
                        }

                        // STEP 4: JournalEntry insert
                        // description mein StockMaster.Id use hota hai — ab guaranteed valid hai
                        var journalEntry = new JournalEntry
                        {
                            current_date = pvm.StockMaster.current_date,
                            due_date = pvm.StockMaster.due_date,
                            posted_date = pvm.StockMaster.posted_date,
                            venderId = pvm.StockMaster.venderId,
                            companyId = companyId,
                            userId = userId,
                            etype = "purchase",
                            description = $"Purchase Entry for StockMaster {pvm.StockMaster.Id}",
                            total_debit = pvm.StockMaster.net_amount,
                            total_credit = pvm.StockMaster.net_amount,
                            fiscalYearId = fiscalYearId // Guaranteed non-null
                        };
                        _context.JournalEntry.Add(journalEntry);
                        await _context.SaveChangesAsync();

                        // ── JournalEntry.Id validate karo ──
                        if (journalEntry.Id <= 0)
                        {
                            await transaction.RollbackAsync();
                            _notyf.Error("Failed to generate Journal Entry ID. Please try again.");
                            return RedirectToAction("PurchaseVoucher");
                        }

                        // STEP 5: JournalDetail (2 rows — Dr/Cr)
                        _context.JournalDetail.AddRange(new List<JournalDetail>
                        {
                            // Purchase Account → DEBIT (Expense badha)
                            new JournalDetail
                            {
                                current_date     = pvm.StockMaster.current_date,
                                journalEntryId   = journalEntry.Id,
                                chartOfAccountId = purchaseAccountId,
                                debit_amount     = pvm.StockMaster.net_amount,
                                credit_amount    = 0.00m,
                                description      = "Purchase Amount"
                            },
                            // Accounts Payable → CREDIT (Liability badhi)
                            new JournalDetail
                            {
                                current_date     = pvm.StockMaster.current_date,
                                journalEntryId   = journalEntry.Id,
                                chartOfAccountId = accountsPayableId,
                                debit_amount     = 0.00m,
                                credit_amount    = pvm.StockMaster.net_amount,
                                description      = $"Payable to Vendor - {vendor?.name}"
                            }
                        });

                        // STEP 6: Ledger (running balance)
                        decimal purchaseRunning = await GetRunningBalance(purchaseAccountId, companyId);
                        decimal payableRunning = await GetRunningBalance(accountsPayableId, companyId);

                        _context.Ledger.AddRange(new List<Ledger>
                        {
                            // Purchase Account (Expense) → running + debit
                            new Ledger
                            {
                                current_date     = pvm.StockMaster.current_date,
                                companyId        = companyId,
                                chartOfAccountId = purchaseAccountId,
                                journalEntryId   = journalEntry.Id,
                                debit_amount     = pvm.StockMaster.net_amount,
                                credit_amount    = 0.00m,
                                running_balance  = purchaseRunning + pvm.StockMaster.net_amount,
                                description      = "Purchase Amount"
                            },
                            // Accounts Payable (Liability) → running + credit
                            new Ledger
                            {
                                current_date     = pvm.StockMaster.current_date,
                                companyId        = companyId,
                                chartOfAccountId = accountsPayableId,
                                journalEntryId   = journalEntry.Id,
                                debit_amount     = 0.00m,
                                credit_amount    = pvm.StockMaster.net_amount,
                                running_balance  = payableRunning + pvm.StockMaster.net_amount,
                                description      = $"Payable to Vendor - {vendor?.name}"
                            }
                        });

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _notyf.Success("Purchase Voucher Created Successfully!");
                    }

                    // ════════════════════════════════════
                    // B) UPDATE EXISTING PURCHASE  (Id > 0)
                    // ════════════════════════════════════
                    else
                    {
                        var existingPurchase = await _context.StockMaster
                            .FirstOrDefaultAsync(x => x.Id == pvm.StockMaster.Id);

                        if (existingPurchase == null)
                        {
                            await transaction.RollbackAsync();
                            _notyf.Error("Purchase record not found.");
                            return RedirectToAction("PurchaseVoucher");
                        }

                        decimal oldNetAmount = existingPurchase.net_amount;

                        // STEP 1: Old StockDetail fetch
                        var oldDetails = await _context.StockDetail
                            .Where(d => d.StockMasterId == existingPurchase.Id)
                            .ToListAsync();

                        // STEP 2: Old item qty REVERSE
                        foreach (var oldDetail in oldDetails)
                        {
                            var oldItem = await _context.Item
                                .FirstOrDefaultAsync(i => i.Id == oldDetail.itemId);
                            if (oldItem != null)
                            {
                                oldItem.qty -= oldDetail.qty;
                                _context.Update(oldItem);
                            }
                        }

                        // STEP 3: Old StockDetail delete
                        _context.StockDetail.RemoveRange(oldDetails);

                        // STEP 4: StockMaster update
                        existingPurchase.current_date = pvm.StockMaster.current_date;
                        existingPurchase.posted_date = pvm.StockMaster.posted_date;
                        existingPurchase.due_date = pvm.StockMaster.due_date;
                        existingPurchase.userId = userId;
                        existingPurchase.companyId = companyId;
                        existingPurchase.venderId = pvm.StockMaster.venderId;
                        existingPurchase.transporterId = pvm.StockMaster.transporterId;
                        existingPurchase.etype = "Purchase";
                        existingPurchase.total_amount = pvm.StockMaster.total_amount;
                        existingPurchase.discount_amount = pvm.StockMaster.discount_amount;
                        existingPurchase.tax_amount = pvm.StockMaster.tax_amount;
                        existingPurchase.net_amount = pvm.StockMaster.net_amount;
                        existingPurchase.remarks = pvm.StockMaster.remarks;
                        existingPurchase.fiscalYearId = fiscalYearId; // Guaranteed non-null
                        _context.Update(existingPurchase);

                        // STEP 5: Vendor balance update (old reverse → new apply)
                        var vendor = await _context.Vender
                            .FirstOrDefaultAsync(v => v.Id == pvm.StockMaster.venderId);
                        if (vendor != null)
                        {
                            vendor.current_balance = vendor.current_balance
                                                   + oldNetAmount                 // Old reverse
                                                   - pvm.StockMaster.net_amount;  // New apply
                            _context.Update(vendor);
                        }

                        // STEP 6: New item qty badhao
                        foreach (var newDetail in pvm.StockDetail)
                        {
                            var item = await _context.Item
                                .FirstOrDefaultAsync(i => i.Id == newDetail.itemId);
                            if (item != null)
                            {
                                item.qty += newDetail.qty;
                                item.purchase_rate = newDetail.rate;
                                item.rate = newDetail.rate;
                                _context.Update(item);
                            }
                        }

                        // STEP 7: New StockDetail add
                        foreach (var detail in pvm.StockDetail)
                        {
                            detail.StockMasterId = existingPurchase.Id;
                            _context.StockDetail.Add(detail);
                        }

                        // STEP 8: Old JournalEntry + JournalDetail + Ledger delete
                        var existingJournalEntry = await _context.JournalEntry
                            .FirstOrDefaultAsync(je =>
                                je.etype == "purchase" &&
                                je.description == $"Purchase Entry for StockMaster {existingPurchase.Id}");

                        if (existingJournalEntry != null)
                        {
                            _context.JournalDetail.RemoveRange(
                                _context.JournalDetail.Where(jd =>
                                    jd.journalEntryId == existingJournalEntry.Id));
                            _context.Ledger.RemoveRange(
                                _context.Ledger.Where(l =>
                                    l.journalEntryId == existingJournalEntry.Id));
                            _context.JournalEntry.Remove(existingJournalEntry);
                        }

                        await _context.SaveChangesAsync();

                        // STEP 9: New JournalEntry
                        var journalEntry = new JournalEntry
                        {
                            current_date = pvm.StockMaster.current_date,
                            due_date = pvm.StockMaster.due_date,
                            posted_date = pvm.StockMaster.posted_date,
                            venderId = pvm.StockMaster.venderId,
                            companyId = companyId,
                            userId = userId,
                            etype = "purchase",
                            description = $"Purchase Entry for StockMaster {existingPurchase.Id}",
                            total_debit = pvm.StockMaster.net_amount,
                            total_credit = pvm.StockMaster.net_amount,
                            fiscalYearId = fiscalYearId // Guaranteed non-null
                        };
                        _context.JournalEntry.Add(journalEntry);
                        await _context.SaveChangesAsync();

                        // ── JournalEntry.Id validate karo ──
                        if (journalEntry.Id <= 0)
                        {
                            await transaction.RollbackAsync();
                            _notyf.Error("Failed to generate Journal Entry ID. Please try again.");
                            return RedirectToAction("PurchaseVoucher");
                        }

                        // STEP 10: New JournalDetail
                        _context.JournalDetail.AddRange(new List<JournalDetail>
                        {
                            new JournalDetail
                            {
                                current_date     = pvm.StockMaster.current_date,
                                journalEntryId   = journalEntry.Id,
                                chartOfAccountId = purchaseAccountId,
                                debit_amount     = pvm.StockMaster.net_amount,
                                credit_amount    = 0.00m,
                                description      = "Purchase Amount"
                            },
                            new JournalDetail
                            {
                                current_date     = pvm.StockMaster.current_date,
                                journalEntryId   = journalEntry.Id,
                                chartOfAccountId = accountsPayableId,
                                debit_amount     = 0.00m,
                                credit_amount    = pvm.StockMaster.net_amount,
                                description      = $"Payable to Vendor - {vendor?.name}"
                            }
                        });

                        // STEP 11: New Ledger
                        decimal purchaseRunning = await GetRunningBalance(purchaseAccountId, companyId);
                        decimal payableRunning = await GetRunningBalance(accountsPayableId, companyId);

                        _context.Ledger.AddRange(new List<Ledger>
                        {
                            new Ledger
                            {
                                current_date     = pvm.StockMaster.current_date,
                                companyId        = companyId,
                                chartOfAccountId = purchaseAccountId,
                                journalEntryId   = journalEntry.Id,
                                debit_amount     = pvm.StockMaster.net_amount,
                                credit_amount    = 0.00m,
                                running_balance  = purchaseRunning + pvm.StockMaster.net_amount,
                                description      = "Purchase Amount"
                            },
                            new Ledger
                            {
                                current_date     = pvm.StockMaster.current_date,
                                companyId        = companyId,
                                chartOfAccountId = accountsPayableId,
                                journalEntryId   = journalEntry.Id,
                                debit_amount     = 0.00m,
                                credit_amount    = pvm.StockMaster.net_amount,
                                running_balance  = payableRunning + pvm.StockMaster.net_amount,
                                description      = $"Payable to Vendor - {vendor?.name}"
                            }
                        });

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _notyf.Success("Purchase Voucher Updated Successfully!");
                    }

                    return RedirectToAction("PurchaseVoucher", new { activeTab = "list" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _notyf.Error($"Transaction Error: {ex.Message}");
                    return BadRequest($"Error: {ex.Message} | Inner: {ex.InnerException?.Message}");
                }
            }
            catch (Exception ex)
            {
                _notyf.Error($"An Error Occurred: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }
        // ════════════════════════════════════════════════════════════════
        // POST: Delete Purchase Voucher
        // ════════════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var purchase = await _context.StockMaster.FindAsync(id);
                if (purchase == null)
                {
                    _notyf.Error("Purchase record not found.");
                    return RedirectToAction("PurchaseVoucher", new { activeTab = "list" });
                }

                // STEP 1: Vendor balance REVERSE (liability ghata → balance wapas + ho)
                var vendor = await _context.Vender
                    .FirstOrDefaultAsync(v => v.Id == purchase.venderId);
                if (vendor != null)
                {
                    vendor.current_balance += purchase.net_amount;
                    _context.Update(vendor);
                }

                // STEP 2: StockDetail fetch
                var details = await _context.StockDetail
                    .Where(d => d.StockMasterId == id)
                    .ToListAsync();

                // STEP 3: Item qty REVERSE (jo purchase ki thi woh wapas ghatao)
                foreach (var detail in details)
                {
                    var item = await _context.Item
                        .FirstOrDefaultAsync(i => i.Id == detail.itemId);
                    if (item != null)
                    {
                        item.qty -= detail.qty;
                        _context.Update(item);
                    }
                }

                // STEP 4: StockDetail delete
                _context.StockDetail.RemoveRange(details);

                // STEP 5: JournalEntry + JournalDetail + Ledger delete
                var journalEntry = await _context.JournalEntry
                    .FirstOrDefaultAsync(je =>
                        je.etype == "purchase" &&
                        je.description == $"Purchase Entry for StockMaster {id}");

                if (journalEntry != null)
                {
                    _context.JournalDetail.RemoveRange(
                        _context.JournalDetail.Where(jd =>
                            jd.journalEntryId == journalEntry.Id));
                    _context.Ledger.RemoveRange(
                        _context.Ledger.Where(l =>
                            l.journalEntryId == journalEntry.Id));
                    _context.JournalEntry.Remove(journalEntry);
                }

                // STEP 6: StockMaster delete
                _context.StockMaster.Remove(purchase);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _notyf.Success("Purchase Voucher Deleted Successfully!");

                return RedirectToAction("PurchaseVoucher", new { activeTab = "list" });
            }
            catch (Exception ex)
            {
                _notyf.Error($"Delete Error: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }
    }
}