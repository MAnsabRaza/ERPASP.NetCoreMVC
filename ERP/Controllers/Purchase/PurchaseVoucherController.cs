using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<IActionResult> PurchaseVoucher(int page = 1, int pageSize = 5, string activeTab = "form")
        {
            var model = new PurchaseViewModel
            {
                StockMaster = new StockMaster
                {
                    current_date = DateOnly.FromDateTime(DateTime.Now),
                    due_date = DateOnly.FromDateTime(DateTime.Now),
                    posted_date = DateOnly.FromDateTime(DateTime.Now)
                },
                StockDetail = new List<StockDetail>()
            };

            int totalPurchase = await _context.StockMaster
                .CountAsync(d => d.etype == "Purchase");

            var purchaseDetail = await _context.StockMaster
                .Where(j => j.etype == "Purchase")
                .OrderBy(j => j.Id)
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

            // Pass pagination info
            ViewBag.TotalItems = totalPurchase;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.ActiveTab = activeTab;

            // Dropdowns
            ViewBag.Warehouses = await _context.Warehouse.ToListAsync();
            ViewBag.Items = await _context.Item.ToListAsync();
            ViewBag.Venders = await _context.Vender.ToListAsync();
            ViewBag.Transporters = await _context.Transporter.ToListAsync();

            ViewBag.Purchase = purchaseDetail; // List<PurchaseListDto>

            return View("~/Views/Purchase/PurchaseVoucher.cshtml", model);
        }
        [HttpGet]
        public async Task<IActionResult> GetPurchaseRate(int itemId)
        {
            try
            {
                var item = await _context.Item.FindAsync(itemId);
                if (item != null)
                {
                    return Json(new { purchaseRate = item.purchase_rate });
                }
                return Json(new { purchaseRate = (decimal?)null });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> Create(PurchaseViewModel pvm)
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
                pvm.StockMaster.companyId = companyId;
                pvm.StockMaster.userId = userId;

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // ── Chart of Accounts fetch ──
                    var purchaseAccount = await _context.ChartOfAccount
                        .FirstOrDefaultAsync(c => c.name == "Purchase Account");
                    var accountsPayableAccount = await _context.ChartOfAccount
                        .FirstOrDefaultAsync(c => c.name == "Accounts Payable");

                    if (purchaseAccount == null || accountsPayableAccount == null)
                    {
                        _notyf.Error("Chart of accounts not found.");
                        return BadRequest("Chart of accounts not found.");
                    }

                    int purchaseAccountId = purchaseAccount.Id;
                    int accountsPayableId = accountsPayableAccount.Id;

                    // ── Running Balance Helper ──
                    async Task<decimal> GetRunningBalance(int chartOfAccountId)
                    {
                        return await _context.Ledger
                            .Where(l => l.chartOfAccountId == chartOfAccountId
                                     && l.companyId == companyId)
                            .OrderByDescending(l => l.Id)
                            .Select(l => l.running_balance)
                            .FirstOrDefaultAsync();
                    }

                    // ════════════════════════════════════
                    // CREATE NEW PURCHASE
                    // ════════════════════════════════════
                    if (pvm.StockMaster.Id == 0)
                    {
                        // STEP 1: StockMaster insert
                        pvm.StockMaster.customerId = null;
                        // ✅ etype waise hi rahega jo form se aaya - koi change nahi
                        _context.StockMaster.Add(pvm.StockMaster);
                        await _context.SaveChangesAsync();

                        // STEP 2: StockDetail + Item qty update
                        foreach (var detail in pvm.StockDetail)
                        {
                            detail.StockMasterId = pvm.StockMaster.Id;
                            _context.StockDetail.Add(detail);

                            // Item qty badhao
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

                        // STEP 3: Vendor balance update
                        var vendor = await _context.Vender
                            .FirstOrDefaultAsync(v => v.Id == pvm.StockMaster.venderId);
                        if (vendor != null)
                        {
                            vendor.current_balance += pvm.StockMaster.net_amount;
                            _context.Update(vendor);
                        }

                        // STEP 4: JournalEntry insert
                        var journalEntry = new JournalEntry
                        {
                            current_date = pvm.StockMaster.current_date,
                            due_date = pvm.StockMaster.due_date,
                            posted_date = pvm.StockMaster.posted_date,
                            companyId = companyId,
                            userId = userId,
                            etype = "purchase",
                            description = $"Purchase Entry for StockMaster {pvm.StockMaster.Id}",
                            total_debit = pvm.StockMaster.net_amount,
                            total_credit = pvm.StockMaster.net_amount
                        };
                        _context.JournalEntry.Add(journalEntry);
                        await _context.SaveChangesAsync();

                        // STEP 5: JournalDetail insert
                        var journalDetails = new List<JournalDetail>
                {
                    // Line 1: Purchase Account DEBIT
                    new JournalDetail
                    {
                        current_date     = pvm.StockMaster.current_date,
                        journalEntryId   = journalEntry.Id,
                        chartOfAccountId = purchaseAccountId,
                        debit_amount     = pvm.StockMaster.net_amount,
                        credit_amount    = 0.00m,
                        description      = "Purchase Amount"
                    },
                    // Line 2: Accounts Payable CREDIT
                    new JournalDetail
                    {
                        current_date     = pvm.StockMaster.current_date,
                        journalEntryId   = journalEntry.Id,
                        chartOfAccountId = accountsPayableId,
                        debit_amount     = 0.00m,
                        credit_amount    = pvm.StockMaster.net_amount,
                        description      = "Payable to Vendor"
                    }
                };
                        _context.JournalDetail.AddRange(journalDetails);

                        // STEP 6: Ledger insert
                        decimal purchaseRunning = await GetRunningBalance(purchaseAccountId);
                        decimal payableRunning = await GetRunningBalance(accountsPayableId);

                        var ledgerEntries = new List<Ledger>
                {
                    // Purchase Account (Expense → running + debit)
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
                    // Accounts Payable (Liability → running + credit)
                    new Ledger
                    {
                        current_date     = pvm.StockMaster.current_date,
                        companyId        = companyId,
                        chartOfAccountId = accountsPayableId,
                        journalEntryId   = journalEntry.Id,
                        debit_amount     = 0.00m,
                        credit_amount    = pvm.StockMaster.net_amount,
                        running_balance  = payableRunning + pvm.StockMaster.net_amount,
                        description      = "Payable to Vendor"
                    }
                };
                        _context.Ledger.AddRange(ledgerEntries);

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _notyf.Success("Purchase Voucher Created Successfully");
                    }

                    // ════════════════════════════════════
                    // UPDATE EXISTING PURCHASE
                    // ════════════════════════════════════
                    else
                    {
                        var existingPurchase = await _context.StockMaster
                            .FirstOrDefaultAsync(x => x.Id == pvm.StockMaster.Id);

                        if (existingPurchase == null)
                        {
                            _notyf.Error("Purchase not found.");
                            return NotFound();
                        }

                        decimal oldNetAmount = existingPurchase.net_amount;

                        // STEP 1: Purani StockDetail fetch
                        var oldDetails = await _context.StockDetail
                            .Where(d => d.StockMasterId == existingPurchase.Id)
                            .ToListAsync();

                        // STEP 2: Purane items ki qty REVERSE karo
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

                        // STEP 3: Purani StockDetail delete
                        _context.StockDetail.RemoveRange(oldDetails);

                        // STEP 4: StockMaster update
                        existingPurchase.current_date = pvm.StockMaster.current_date;
                        existingPurchase.posted_date = pvm.StockMaster.posted_date;
                        existingPurchase.due_date = pvm.StockMaster.due_date;
                        existingPurchase.userId = userId;
                        existingPurchase.companyId = companyId;
                        existingPurchase.venderId = pvm.StockMaster.venderId;
                        existingPurchase.transporterId = pvm.StockMaster.transporterId;
                        existingPurchase.etype = pvm.StockMaster.etype;
                        existingPurchase.total_amount = pvm.StockMaster.total_amount;
                        existingPurchase.discount_amount = pvm.StockMaster.discount_amount;
                        existingPurchase.tax_amount = pvm.StockMaster.tax_amount;
                        existingPurchase.net_amount = pvm.StockMaster.net_amount;
                        existingPurchase.remarks = pvm.StockMaster.remarks;
                        _context.Update(existingPurchase);

                        // STEP 5: Vendor balance update
                        var vendor = await _context.Vender
                            .FirstOrDefaultAsync(v => v.Id == pvm.StockMaster.venderId);
                        if (vendor != null)
                        {
                            vendor.current_balance = vendor.current_balance
                                                   - oldNetAmount
                                                   + pvm.StockMaster.net_amount;
                            _context.Update(vendor);
                        }

                        // STEP 6: Naye items ki qty badhao
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

                        // STEP 7: Naye StockDetail add
                        foreach (var detail in pvm.StockDetail)
                        {
                            detail.StockMasterId = existingPurchase.Id;
                            _context.StockDetail.Add(detail);
                        }

                        // STEP 8: Purani JournalEntry/Detail/Ledger delete
                        var existingJournalEntry = await _context.JournalEntry
                            .FirstOrDefaultAsync(je => je.description ==
                                $"Purchase Entry for StockMaster {existingPurchase.Id}");

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

                        // STEP 9: Naya JournalEntry
                        var journalEntry = new JournalEntry
                        {
                            current_date = pvm.StockMaster.current_date,
                            due_date = pvm.StockMaster.due_date,
                            posted_date = pvm.StockMaster.posted_date,
                            companyId = companyId,
                            userId = userId,
                            etype = "purchase",
                            description = $"Purchase Entry for StockMaster {existingPurchase.Id}",
                            total_debit = pvm.StockMaster.net_amount,
                            total_credit = pvm.StockMaster.net_amount
                        };
                        _context.JournalEntry.Add(journalEntry);
                        await _context.SaveChangesAsync();

                        // STEP 10: JournalDetail
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
                        description      = "Payable to Vendor"
                    }
                });

                        // STEP 11: Ledger
                        decimal purchaseRunning = await GetRunningBalance(purchaseAccountId);
                        decimal payableRunning = await GetRunningBalance(accountsPayableId);

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
                        description      = "Payable to Vendor"
                    }
                });

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _notyf.Success("Purchase Voucher Updated Successfully");
                    }

                    return RedirectToAction("PurchaseVoucher");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Error saving purchase voucher: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                _notyf.Error($"An Error Occurred: {ex.Message}");
                var inner = ex.InnerException != null ? ex.InnerException.Message : "";
                return BadRequest($"{ex.Message} - {inner}");
            }
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var purchase = await _context.StockMaster.FindAsync(id);
                if (purchase != null)
                {
                    // STEP 1: Vendor balance update
                    var vendor = await _context.Vender
                        .FirstOrDefaultAsync(v => v.Id == purchase.venderId);
                    if (vendor != null)
                    {
                        vendor.current_balance -= purchase.net_amount;
                        _context.Update(vendor);
                    }

                    // STEP 2: StockDetail fetch karo
                    var details = await _context.StockDetail
                        .Where(d => d.StockMasterId == id)
                        .ToListAsync();

                    // ✅ STEP 3: Har item ki qty WAPAS GHATAO
                    foreach (var detail in details)
                    {
                        var item = await _context.Item
                            .FirstOrDefaultAsync(i => i.Id == detail.itemId);
                        if (item != null)
                        {
                            item.qty -= detail.qty; // jo purchase ki thi woh wapas hatao
                            _context.Update(item);
                        }
                    }

                    // STEP 4: StockDetail delete karo
                    _context.StockDetail.RemoveRange(details);

                    // STEP 5: JournalEntry, JournalDetail, Ledger delete
                    var journalEntry = await _context.JournalEntry
                        .FirstOrDefaultAsync(je => je.etype == "purchase"
                            && je.description == $"Purchase Entry for StockMaster {id}");
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
                    _notyf.Success("Purchase Voucher Deleted Successfully");
                }

                return RedirectToAction("PurchaseVoucher");
            }
            catch (Exception ex)
            {
                _notyf.Error($"An Error Occurred: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id, int page = 1, int pageSize = 5)
        {
            var purchase = await _context.StockMaster
                   .Include(u => u.User)
                   .Include(v => v.Vender)
                     .Include(t => t.Transporter)
                  .Include(j => j.Company)
                  .FirstOrDefaultAsync(j => j.Id == id);

            if (purchase == null)
            {
                return NotFound();
            }
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

            int totalPurchase = await _context.StockMaster
                .CountAsync(d => d.etype == "Purchase");

            var purchaseData = await _context.StockMaster
                .Where(j => j.etype == "Purchase")
                .OrderBy(j => j.Id)
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


            ViewBag.Warehouses = await _context.Warehouse.ToListAsync();
            ViewBag.Items = await _context.Item.ToListAsync();
            ViewBag.Venders = await _context.Vender.ToListAsync();
            ViewBag.Transporters = await _context.Transporter.ToListAsync();

            ViewBag.Purchase = purchaseData;

            return View("~/Views/Purchase/PurchaseVoucher.cshtml", model);
        }
        [HttpGet]
        public async Task<IActionResult> ItemModal()
        {
            var model = new Item
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };

            ViewBag.categoryList = await _context.Category.Where(c => c.status == true).ToListAsync();
            ViewBag.brandList = await _context.Brand.Where(b => b.status == true).ToListAsync();
            ViewBag.uomList = await _context.UOM.Where(u => u.status == true).ToListAsync();
            ViewBag.subCategoryList = await _context.SubCategory.Where(sb => sb.status == true).ToListAsync();
            return PartialView("~/Views/Shared/_ItemModal.cshtml", model);
        }

    }
}