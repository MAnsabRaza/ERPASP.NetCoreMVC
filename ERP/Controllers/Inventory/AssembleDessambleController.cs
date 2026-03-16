using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ERP.Controllers.Purchase
{
    public class AssembleDessambleController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;

        public AssembleDessambleController(AppDbContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }

        public async Task<IActionResult> AssembleDessamble(int page = 1, int pageSize = 5, string activeTab = "form")
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
                .CountAsync(d => d.etype == "StockAdjustment_In" || d.etype == "StockAdjustment_Out");

            var purchaseDetail = await _context.StockMaster
                .Where(j => j.etype == "StockAdjustment_In" || j.etype == "StockAdjustment_Out")
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
            ViewBag.ActiveTab = activeTab;

            ViewBag.Warehouses = await _context.Warehouse.ToListAsync();
            ViewBag.Items = await _context.Item.ToListAsync();
            ViewBag.Venders = await _context.Vender.ToListAsync();
            ViewBag.Transporters = await _context.Transporter.ToListAsync();
            ViewBag.PurchaseReturn = purchaseDetail;

            return View("~/Views/Inventory/AssembleDessamble.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetPurchaseRate(int itemId)
        {
            try
            {
                var item = await _context.Item.FindAsync(itemId);
                if (item != null)
                    return Json(new { purchaseRate = item.purchase_rate });
                return Json(new { purchaseRate = (decimal?)null });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckItemQuantity(int itemId, decimal qty)
        {
            try
            {
                var item = await _context.Item.FindAsync(itemId);
                if (item == null)
                    return Json(new { success = false, message = $"Item not found in database." });

                if (item.qty <= 0)
                    return Json(new { success = false, message = $"'{item.item_name}' has zero quantity in stock." });

                if (qty > item.qty)
                    return Json(new { success = false, message = $"'{item.item_name}' available quantity is {item.qty}." });

                return Json(new { success = true, message = $"Item added successfully." });
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

                // ════════════════════════════════════════
                // Determine Stock In or Stock Out
                // ════════════════════════════════════════
                bool isStockIn = pvm.StockMaster.etype == "StockAdjustment_In";

                // ════════════════════════════════════════
                // Stock OUT: Server-side Qty Validation
                // ════════════════════════════════════════
                if (!isStockIn)
                {
                    foreach (var detail in pvm.StockDetail)
                    {
                        var item = await _context.Item.FirstOrDefaultAsync(i => i.Id == detail.itemId);
                        if (item == null)
                        {
                            _notyf.Error($"Item ID {detail.itemId} not found.");
                            return RedirectToAction("AssembleDessamble");
                        }
                        if (item.qty <= 0)
                        {
                            _notyf.Error($"'{item.item_name}' has zero quantity in stock.");
                            return RedirectToAction("AssembleDessamble");
                        }
                        if (detail.qty > item.qty)
                        {
                            _notyf.Error($"'{item.item_name}' available quantity is {item.qty}.");
                            return RedirectToAction("AssembleDessamble");
                        }
                    }
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // ── Chart of Accounts fetch ──
                    var inventoryAccount = await _context.ChartOfAccount
                        .FirstOrDefaultAsync(c => c.name == "Inventory");

                    var cogsAccount = await _context.ChartOfAccount
                        .FirstOrDefaultAsync(c => c.name == "Cost of Goods Sold");

                    if (inventoryAccount == null || cogsAccount == null)
                    {
                        _notyf.Error("Chart of accounts (Inventory / COGS) not found.");
                        return BadRequest("Chart of accounts not found.");
                    }

                    int inventoryAccountId = inventoryAccount.Id;   // 39
                    int cogsAccountId = cogsAccount.Id;             // 35

                    async Task<decimal> GetRunningBalance(int chartOfAccountId)
                    {
                        return await _context.Ledger
                            .Where(l => l.chartOfAccountId == chartOfAccountId && l.companyId == companyId)
                            .OrderByDescending(l => l.Id)
                            .Select(l => l.running_balance)
                            .FirstOrDefaultAsync();
                    }

                    // ════════════════════════════════════
                    // CREATE NEW STOCK ADJUSTMENT
                    // ════════════════════════════════════
                    if (pvm.StockMaster.Id == 0)
                    {
                        // STEP 1: StockMaster insert
                        pvm.StockMaster.customerId = null;
                        pvm.StockMaster.venderId = null;
                        _context.StockMaster.Add(pvm.StockMaster);
                        await _context.SaveChangesAsync();

                        // STEP 2: StockDetail + Item qty update
                        foreach (var detail in pvm.StockDetail)
                        {
                            detail.StockMasterId = pvm.StockMaster.Id;

                            // Stock In  → qty positive (+)
                            // Stock Out → qty negative (-)
                            if (!isStockIn)
                                detail.qty = -Math.Abs(detail.qty);
                            else
                                detail.qty = Math.Abs(detail.qty);

                            _context.StockDetail.Add(detail);

                            var item = await _context.Item.FirstOrDefaultAsync(i => i.Id == detail.itemId);
                            if (item != null)
                            {
                                if (isStockIn)
                                    item.qty += Math.Abs(detail.qty);   // ✅ INCREASE
                                else
                                    item.qty -= Math.Abs(detail.qty);   // ✅ DECREASE
                                _context.Update(item);
                            }
                        }

                        // STEP 3: JournalEntry insert
                        string journalDesc = isStockIn
                            ? $"Stock In Adjustment for StockMaster {pvm.StockMaster.Id}"
                            : $"Stock Out Adjustment for StockMaster {pvm.StockMaster.Id}";

                        var journalEntry = new JournalEntry
                        {
                            current_date = pvm.StockMaster.current_date,
                            due_date = pvm.StockMaster.due_date,
                            posted_date = pvm.StockMaster.posted_date,
                            companyId = companyId,
                            venderId = null,
                            userId = userId,
                            etype = pvm.StockMaster.etype,
                            description = journalDesc,
                            total_debit = pvm.StockMaster.net_amount,
                            total_credit = pvm.StockMaster.net_amount
                        };
                        _context.JournalEntry.Add(journalEntry);
                        await _context.SaveChangesAsync();

                        // STEP 4: JournalDetail
                        // STOCK IN:  Inventory DEBIT  | COGS CREDIT
                        // STOCK OUT: COGS DEBIT        | Inventory CREDIT
                        _context.JournalDetail.AddRange(new List<JournalDetail>
                        {
                            new JournalDetail
                            {
                                current_date     = pvm.StockMaster.current_date,
                                journalEntryId   = journalEntry.Id,
                                chartOfAccountId = isStockIn ? inventoryAccountId : cogsAccountId,
                                debit_amount     = pvm.StockMaster.net_amount,
                                credit_amount    = 0.00m,
                                description      = isStockIn ? "Stock In - Inventory Increased" : "Stock Out - Loss/Expense"
                            },
                            new JournalDetail
                            {
                                current_date     = pvm.StockMaster.current_date,
                                journalEntryId   = journalEntry.Id,
                                chartOfAccountId = isStockIn ? cogsAccountId : inventoryAccountId,
                                debit_amount     = 0.00m,
                                credit_amount    = pvm.StockMaster.net_amount,
                                description      = isStockIn ? "Stock In - COGS/Gain Credit" : "Stock Out - Inventory Reduced"
                            }
                        });

                        // STEP 5: Ledger
                        decimal inventoryRunning = await GetRunningBalance(inventoryAccountId);
                        decimal cogsRunning = await GetRunningBalance(cogsAccountId);

                        _context.Ledger.AddRange(new List<Ledger>
                        {
                            new Ledger
                            {
                                current_date     = pvm.StockMaster.current_date,
                                companyId        = companyId,
                                chartOfAccountId = isStockIn ? inventoryAccountId : cogsAccountId,
                                journalEntryId   = journalEntry.Id,
                                debit_amount     = pvm.StockMaster.net_amount,
                                credit_amount    = 0.00m,
                                running_balance  = isStockIn
                                                    ? inventoryRunning + pvm.StockMaster.net_amount
                                                    : cogsRunning + pvm.StockMaster.net_amount,
                                description      = isStockIn ? "Stock In - Inventory Increased" : "Stock Out - Loss/Expense"
                            },
                            new Ledger
                            {
                                current_date     = pvm.StockMaster.current_date,
                                companyId        = companyId,
                                chartOfAccountId = isStockIn ? cogsAccountId : inventoryAccountId,
                                journalEntryId   = journalEntry.Id,
                                debit_amount     = 0.00m,
                                credit_amount    = pvm.StockMaster.net_amount,
                                running_balance  = isStockIn
                                                    ? cogsRunning + pvm.StockMaster.net_amount
                                                    : inventoryRunning - pvm.StockMaster.net_amount,
                                description      = isStockIn ? "Stock In - COGS/Gain Credit" : "Stock Out - Inventory Reduced"
                            }
                        });

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _notyf.Success(isStockIn ? "Stock In Saved Successfully" : "Stock Out Saved Successfully");
                    }

                    // ════════════════════════════════════
                    // UPDATE EXISTING STOCK ADJUSTMENT
                    // ════════════════════════════════════
                    else
                    {
                        var existingMaster = await _context.StockMaster
                            .FirstOrDefaultAsync(x => x.Id == pvm.StockMaster.Id);

                        if (existingMaster == null)
                        {
                            _notyf.Error("Record not found.");
                            return NotFound();
                        }

                        bool wasStockIn = existingMaster.etype == "StockAdjustment_In";
                        decimal oldNetAmount = existingMaster.net_amount;

                        // STEP 1: Purani StockDetail fetch
                        var oldDetails = await _context.StockDetail
                            .Where(d => d.StockMasterId == existingMaster.Id)
                            .ToListAsync();

                        // STEP 2: Reverse purani qty
                        foreach (var oldDetail in oldDetails)
                        {
                            var oldItem = await _context.Item.FirstOrDefaultAsync(i => i.Id == oldDetail.itemId);
                            if (oldItem != null)
                            {
                                if (wasStockIn)
                                    oldItem.qty -= Math.Abs(oldDetail.qty);  // undo stock in
                                else
                                    oldItem.qty += Math.Abs(oldDetail.qty);  // undo stock out
                                _context.Update(oldItem);
                            }
                        }

                        // STEP 3: Purani StockDetail delete
                        _context.StockDetail.RemoveRange(oldDetails);

                        // STEP 4: StockMaster update
                        existingMaster.current_date = pvm.StockMaster.current_date;
                        existingMaster.posted_date = pvm.StockMaster.posted_date;
                        existingMaster.due_date = pvm.StockMaster.due_date;
                        existingMaster.userId = userId;
                        existingMaster.companyId = companyId;
                        existingMaster.venderId = null;
                        existingMaster.etype = pvm.StockMaster.etype;
                        existingMaster.total_amount = pvm.StockMaster.total_amount;
                        existingMaster.discount_amount = pvm.StockMaster.discount_amount;
                        existingMaster.tax_amount = pvm.StockMaster.tax_amount;
                        existingMaster.net_amount = pvm.StockMaster.net_amount;
                        existingMaster.remarks = pvm.StockMaster.remarks;
                        _context.Update(existingMaster);

                        // STEP 5: Naye items ki qty apply
                        foreach (var newDetail in pvm.StockDetail)
                        {
                            if (!isStockIn)
                                newDetail.qty = -Math.Abs(newDetail.qty);
                            else
                                newDetail.qty = Math.Abs(newDetail.qty);

                            var item = await _context.Item.FirstOrDefaultAsync(i => i.Id == newDetail.itemId);
                            if (item != null)
                            {
                                if (isStockIn)
                                    item.qty += Math.Abs(newDetail.qty);
                                else
                                    item.qty -= Math.Abs(newDetail.qty);
                                _context.Update(item);
                            }
                        }

                        // STEP 6: Naye StockDetail add
                        foreach (var detail in pvm.StockDetail)
                        {
                            detail.StockMasterId = existingMaster.Id;
                            _context.StockDetail.Add(detail);
                        }

                        // STEP 7: Purani JournalEntry/Detail/Ledger delete
                        var existingJournal = await _context.JournalEntry
                            .FirstOrDefaultAsync(je =>
                                je.description.Contains($"StockMaster {existingMaster.Id}") &&
                                (je.etype == "StockAdjustment_In" || je.etype == "StockAdjustment_Out"));

                        if (existingJournal != null)
                        {
                            _context.JournalDetail.RemoveRange(
                                _context.JournalDetail.Where(jd => jd.journalEntryId == existingJournal.Id));
                            _context.Ledger.RemoveRange(
                                _context.Ledger.Where(l => l.journalEntryId == existingJournal.Id));
                            _context.JournalEntry.Remove(existingJournal);
                        }

                        await _context.SaveChangesAsync();

                        // STEP 8: Naya JournalEntry
                        string journalDesc = isStockIn
                            ? $"Stock In Adjustment for StockMaster {existingMaster.Id}"
                            : $"Stock Out Adjustment for StockMaster {existingMaster.Id}";

                        var journalEntry = new JournalEntry
                        {
                            current_date = pvm.StockMaster.current_date,
                            due_date = pvm.StockMaster.due_date,
                            posted_date = pvm.StockMaster.posted_date,
                            companyId = companyId,
                            venderId = null,
                            userId = userId,
                            etype = pvm.StockMaster.etype,
                            description = journalDesc,
                            total_debit = pvm.StockMaster.net_amount,
                            total_credit = pvm.StockMaster.net_amount
                        };
                        _context.JournalEntry.Add(journalEntry);
                        await _context.SaveChangesAsync();

                        // STEP 9: JournalDetail
                        _context.JournalDetail.AddRange(new List<JournalDetail>
                        {
                            new JournalDetail
                            {
                                current_date     = pvm.StockMaster.current_date,
                                journalEntryId   = journalEntry.Id,
                                chartOfAccountId = isStockIn ? inventoryAccountId : cogsAccountId,
                                debit_amount     = pvm.StockMaster.net_amount,
                                credit_amount    = 0.00m,
                                description      = isStockIn ? "Stock In - Inventory Increased" : "Stock Out - Loss/Expense"
                            },
                            new JournalDetail
                            {
                                current_date     = pvm.StockMaster.current_date,
                                journalEntryId   = journalEntry.Id,
                                chartOfAccountId = isStockIn ? cogsAccountId : inventoryAccountId,
                                debit_amount     = 0.00m,
                                credit_amount    = pvm.StockMaster.net_amount,
                                description      = isStockIn ? "Stock In - COGS/Gain Credit" : "Stock Out - Inventory Reduced"
                            }
                        });

                        // STEP 10: Ledger
                        decimal inventoryRunning = await GetRunningBalance(inventoryAccountId);
                        decimal cogsRunning = await GetRunningBalance(cogsAccountId);

                        _context.Ledger.AddRange(new List<Ledger>
                        {
                            new Ledger
                            {
                                current_date     = pvm.StockMaster.current_date,
                                companyId        = companyId,
                                chartOfAccountId = isStockIn ? inventoryAccountId : cogsAccountId,
                                journalEntryId   = journalEntry.Id,
                                debit_amount     = pvm.StockMaster.net_amount,
                                credit_amount    = 0.00m,
                                running_balance  = isStockIn
                                                    ? inventoryRunning + pvm.StockMaster.net_amount
                                                    : cogsRunning + pvm.StockMaster.net_amount,
                                description      = isStockIn ? "Stock In - Inventory Increased" : "Stock Out - Loss/Expense"
                            },
                            new Ledger
                            {
                                current_date     = pvm.StockMaster.current_date,
                                companyId        = companyId,
                                chartOfAccountId = isStockIn ? cogsAccountId : inventoryAccountId,
                                journalEntryId   = journalEntry.Id,
                                debit_amount     = 0.00m,
                                credit_amount    = pvm.StockMaster.net_amount,
                                running_balance  = isStockIn
                                                    ? cogsRunning + pvm.StockMaster.net_amount
                                                    : inventoryRunning - pvm.StockMaster.net_amount,
                                description      = isStockIn ? "Stock In - COGS/Gain Credit" : "Stock Out - Inventory Reduced"
                            }
                        });

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _notyf.Success(isStockIn ? "Stock In Updated Successfully" : "Stock Out Updated Successfully");
                    }

                    return RedirectToAction("AssembleDessamble", new { activeTab = "list" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Error saving stock adjustment: {ex.Message}", ex);
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int page = 1, int pageSize = 5)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var stockMaster = await _context.StockMaster.FirstOrDefaultAsync(s => s.Id == id);
                if (stockMaster == null)
                {
                    _notyf.Error("Record not found.");
                    return RedirectToAction("AssembleDessamble");
                }

                bool wasStockIn = stockMaster.etype == "StockAdjustment_In";

                // STEP 1: Reverse Item qty
                var stockDetails = await _context.StockDetail
                    .Where(d => d.StockMasterId == id)
                    .ToListAsync();

                foreach (var detail in stockDetails)
                {
                    var item = await _context.Item.FirstOrDefaultAsync(i => i.Id == detail.itemId);
                    if (item != null)
                    {
                        if (wasStockIn)
                            item.qty -= Math.Abs(detail.qty);   // undo stock in
                        else
                            item.qty += Math.Abs(detail.qty);   // undo stock out
                        _context.Update(item);
                    }
                }

                // STEP 2: Delete StockDetail
                _context.StockDetail.RemoveRange(stockDetails);

                // STEP 3: Delete Ledger, JournalDetail, JournalEntry
                var journal = await _context.JournalEntry
                    .FirstOrDefaultAsync(j =>
                        j.description.Contains($"StockMaster {id}") &&
                        (j.etype == "StockAdjustment_In" || j.etype == "StockAdjustment_Out"));

                if (journal != null)
                {
                    _context.Ledger.RemoveRange(_context.Ledger.Where(l => l.journalEntryId == journal.Id));
                    _context.JournalDetail.RemoveRange(_context.JournalDetail.Where(d => d.journalEntryId == journal.Id));
                    _context.JournalEntry.Remove(journal);
                }

                // STEP 4: Delete StockMaster
                _context.StockMaster.Remove(stockMaster);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _notyf.Success("Stock Adjustment Deleted Successfully");
                return RedirectToAction("AssembleDessamble", new { page, pageSize, activeTab = "list" });
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("AssembleDessamble");
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
                return NotFound();

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
                .CountAsync(d => d.etype == "StockAdjustment_In" || d.etype == "StockAdjustment_Out");

            var purchaseData = await _context.StockMaster
                .Where(j => j.etype == "StockAdjustment_In" || j.etype == "StockAdjustment_Out")
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
            ViewBag.Warehouses = await _context.Warehouse.ToListAsync();
            ViewBag.Items = await _context.Item.ToListAsync();
            ViewBag.Venders = await _context.Vender.ToListAsync();
            ViewBag.Transporters = await _context.Transporter.ToListAsync();
            ViewBag.Purchase = purchaseData;

            return View("~/Views/Inventory/AssembleDessamble.cshtml", model);
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