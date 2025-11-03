using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers.Purchase
{
    public class PurchaseReturnVoucherController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;

        public PurchaseReturnVoucherController(AppDbContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }

        public async Task<IActionResult> PurchaseReturnVoucher(int page = 1, int pageSize = 5, string activeTab = "form")
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

            int totalPurchaseReturn = await _context.StockMaster
                .CountAsync(d => d.etype == "PurchaseReturn");

            var purchaseReturnDetail = await _context.StockMaster
                .Where(j => j.etype == "PurchaseReturn")
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

            ViewBag.TotalItems = totalPurchaseReturn;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.ActiveTab = activeTab;
            ViewBag.Warehouses = await _context.Warehouse.ToListAsync();
            ViewBag.Items = await _context.Item.ToListAsync();
            ViewBag.Venders = await _context.Vender.ToListAsync();
            ViewBag.Transporters = await _context.Transporter.ToListAsync();
            ViewBag.PurchaseReturn = purchaseReturnDetail;

            return View("~/Views/Purchase/PurchaseReturnVoucher.cshtml", model);
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseViewModel pvm)
        {
            try
            {
                // Validate session
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
                    // Get chart of accounts
                    var inventoryAccount = await _context.ChartOfAccount
                        .FirstOrDefaultAsync(c => c.name == "Inventory" && c.companyId == companyId);
                    var accountsPayableAccount = await _context.ChartOfAccount
                        .FirstOrDefaultAsync(c => c.name == "Account Payable" && c.companyId == companyId);

                    if (inventoryAccount == null || accountsPayableAccount == null)
                    {
                        _notyf.Error("Required chart of accounts not found. Please set up Inventory and Account Payable accounts.");
                        return RedirectToAction("PurchaseReturnVoucher");
                    }

                    int inventoryAccountId = inventoryAccount.Id;
                    int accountsPayableAccountId = accountsPayableAccount.Id;

                    if (pvm.StockMaster.Id > 0)
                    {
                        // UPDATE EXISTING PURCHASE RETURN
                        await UpdatePurchaseReturn(pvm, companyId, userId, inventoryAccountId, accountsPayableAccountId);
                    }
                    else
                    {
                        // CREATE NEW PURCHASE RETURN
                        await CreateNewPurchaseReturn(pvm, companyId, userId, inventoryAccountId, accountsPayableAccountId);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _notyf.Success(pvm.StockMaster.Id > 0
                        ? "Purchase Return Voucher Updated Successfully"
                        : "Purchase Return Voucher Created Successfully");

                    return RedirectToAction("PurchaseReturnVoucher", new { activeTab = "list" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _notyf.Error($"Transaction Error: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _notyf.Error($"An Error Occurred: {ex.Message}");
                var inner = ex.InnerException != null ? ex.InnerException.Message : "";
                return BadRequest($"{ex.Message} - {inner}");
            }
        }

        private async Task UpdatePurchaseReturn(PurchaseViewModel pvm, int companyId, int userId,
            int inventoryAccountId, int accountsPayableAccountId)
        {
            var existingPurchaseReturn = await _context.StockMaster
                .FirstOrDefaultAsync(x => x.Id == pvm.StockMaster.Id);

            if (existingPurchaseReturn == null)
            {
                throw new Exception("Purchase Return not found");
            }

            decimal oldNetAmount = existingPurchaseReturn.net_amount;
            int? oldVenderId = existingPurchaseReturn.venderId;

            // Update StockMaster
            existingPurchaseReturn.current_date = pvm.StockMaster.current_date;
            existingPurchaseReturn.posted_date = pvm.StockMaster.posted_date;
            existingPurchaseReturn.due_date = pvm.StockMaster.due_date;
            existingPurchaseReturn.userId = userId;
            existingPurchaseReturn.companyId = companyId;
            existingPurchaseReturn.venderId = pvm.StockMaster.venderId;
            existingPurchaseReturn.transporterId = pvm.StockMaster.transporterId;
            existingPurchaseReturn.etype = "PurchaseReturn";
            existingPurchaseReturn.total_amount = pvm.StockMaster.total_amount;
            existingPurchaseReturn.discount_amount = pvm.StockMaster.discount_amount;
            existingPurchaseReturn.tax_amount = pvm.StockMaster.tax_amount;
            existingPurchaseReturn.net_amount = pvm.StockMaster.net_amount;
            existingPurchaseReturn.remarks = pvm.StockMaster.remarks;

            _context.Update(existingPurchaseReturn);

            // Update vendor balances (reverse old, apply new)
            if (oldVenderId.HasValue)
            {
                var oldVendor = await _context.Vender.FindAsync(oldVenderId.Value);
                if (oldVendor != null)
                {
                    // Reverse old entry: subtract the old return amount
                    oldVendor.current_balance -= oldNetAmount;
                    _context.Update(oldVendor);
                }
            }

            if (pvm.StockMaster.venderId.HasValue)
            {
                var newVendor = await _context.Vender.FindAsync(pvm.StockMaster.venderId.Value);
                if (newVendor != null)
                {
                    // Apply new entry: add the new return amount
                    newVendor.current_balance += pvm.StockMaster.net_amount;
                    _context.Update(newVendor);
                }
            }

            // Remove existing details
            var existingDetails = _context.StockDetail
                .Where(d => d.stockMasterId == existingPurchaseReturn.Id);
            _context.StockDetail.RemoveRange(existingDetails);

            // Remove existing journal entries
            var existingJournalEntry = await _context.JournalEntry
                .FirstOrDefaultAsync(je => je.etype == "PurchaseReturn"
                    && je.description.Contains($"StockMaster {existingPurchaseReturn.Id}"));

            if (existingJournalEntry != null)
            {
                var existingJournalDetails = _context.JournalDetail
                    .Where(jd => jd.journalEntryId == existingJournalEntry.Id);
                var existingLedgerEntries = _context.Ledger
                    .Where(l => l.journalEntryId == existingJournalEntry.Id);

                _context.JournalDetail.RemoveRange(existingJournalDetails);
                _context.Ledger.RemoveRange(existingLedgerEntries);
                _context.JournalEntry.Remove(existingJournalEntry);
            }

            // Create new journal entries
            await CreateJournalEntries(pvm, existingPurchaseReturn.Id, companyId, userId,
                inventoryAccountId, accountsPayableAccountId);

            // Add new stock details
            foreach (var detail in pvm.StockDetail)
            {
                detail.stockMasterId = existingPurchaseReturn.Id;
                _context.StockDetail.Add(detail);
            }
        }

        private async Task CreateNewPurchaseReturn(PurchaseViewModel pvm, int companyId, int userId,
            int inventoryAccountId, int accountsPayableAccountId)
        {
            // Add StockMaster
            pvm.StockMaster.customerId = null;
            pvm.StockMaster.etype = "PurchaseReturn";
            _context.StockMaster.Add(pvm.StockMaster);
            await _context.SaveChangesAsync();

            // Update vendor balance - INCREASE balance (we're returning goods, vendor owes us less)
            if (pvm.StockMaster.venderId.HasValue)
            {
                var vendor = await _context.Vender.FindAsync(pvm.StockMaster.venderId.Value);
                if (vendor != null)
                {
                    vendor.current_balance += pvm.StockMaster.net_amount;
                    _context.Update(vendor);
                }
            }

            // Create journal entries
            await CreateJournalEntries(pvm, pvm.StockMaster.Id, companyId, userId,
                inventoryAccountId, accountsPayableAccountId);

            // Add stock details
            foreach (var detail in pvm.StockDetail)
            {
                detail.stockMasterId = pvm.StockMaster.Id;
                _context.StockDetail.Add(detail);
            }
        }

        private async Task CreateJournalEntries(PurchaseViewModel pvm, int stockMasterId,
            int companyId, int userId, int inventoryAccountId, int accountsPayableAccountId)
        {
            // Create JournalEntry
            var journalEntry = new JournalEntry
            {
                current_date = pvm.StockMaster.current_date,
                due_date = pvm.StockMaster.due_date,
                posted_date = pvm.StockMaster.posted_date,
                companyId = companyId,
                userId = userId,
                etype = "PurchaseReturn",
                description = $"Purchase Return Entry for StockMaster {stockMasterId}",
                total_debit = pvm.StockMaster.net_amount,
                total_credit = pvm.StockMaster.net_amount
            };
            _context.JournalEntry.Add(journalEntry);
            await _context.SaveChangesAsync();

            // CORRECTED ACCOUNTING LOGIC FOR PURCHASE RETURN:
            // Debit: Accounts Payable (decrease liability)
            // Credit: Inventory (decrease asset)

            // Create JournalDetail entries
            var journalDetails = new List<JournalDetail>
            {
                new JournalDetail
                {
                    current_date = pvm.StockMaster.current_date,
                    journalEntryId = journalEntry.Id,
                    chartOfAccountId = accountsPayableAccountId,
                    debit_amount = pvm.StockMaster.net_amount,
                    credit_amount = 0.00m,
                    description = "Purchase Return - Reduce Liability"
                },
                new JournalDetail
                {
                    current_date = pvm.StockMaster.current_date,
                    journalEntryId = journalEntry.Id,
                    chartOfAccountId = inventoryAccountId,
                    debit_amount = 0.00m,
                    credit_amount = pvm.StockMaster.net_amount,
                    description = "Purchase Return - Reduce Inventory"
                }
            };
            _context.JournalDetail.AddRange(journalDetails);

            // Get current running balances
            var inventoryBalance = await _context.Ledger
                .Where(l => l.chartOfAccountId == inventoryAccountId && l.companyId == companyId)
                .OrderByDescending(l => l.Id)
                .Select(l => l.running_balance)
                .FirstOrDefaultAsync();

            var payableBalance = await _context.Ledger
                .Where(l => l.chartOfAccountId == accountsPayableAccountId && l.companyId == companyId)
                .OrderByDescending(l => l.Id)
                .Select(l => l.running_balance)
                .FirstOrDefaultAsync();

            // Create Ledger entries
            var ledgerEntries = new List<Ledger>
            {
                new Ledger
                {
                    current_date = pvm.StockMaster.current_date,
                    companyId = companyId,
                    chartOfAccountId = accountsPayableAccountId,
                    journalEntryId = journalEntry.Id,
                    debit_amount = pvm.StockMaster.net_amount,
                    credit_amount = 0.00m,
                    running_balance = payableBalance - pvm.StockMaster.net_amount,
                    description = "Purchase Return - Reduce Liability"
                },
                new Ledger
                {
                    current_date = pvm.StockMaster.current_date,
                    companyId = companyId,
                    chartOfAccountId = inventoryAccountId,
                    journalEntryId = journalEntry.Id,
                    debit_amount = 0.00m,
                    credit_amount = pvm.StockMaster.net_amount,
                    running_balance = inventoryBalance - pvm.StockMaster.net_amount,
                    description = "Purchase Return - Reduce Inventory"
                }
            };
            _context.Ledger.AddRange(ledgerEntries);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var purchaseReturn = await _context.StockMaster.FindAsync(id);
                if (purchaseReturn == null)
                {
                    _notyf.Error("Purchase Return not found");
                    return RedirectToAction("PurchaseReturnVoucher");
                }

                // Reverse vendor balance
                if (purchaseReturn.venderId.HasValue)
                {
                    var vendor = await _context.Vender.FindAsync(purchaseReturn.venderId.Value);
                    if (vendor != null)
                    {
                        vendor.current_balance -= purchaseReturn.net_amount;
                        _context.Update(vendor);
                    }
                }

                // Delete journal entries
                var journalEntry = await _context.JournalEntry
                    .FirstOrDefaultAsync(je => je.etype == "PurchaseReturn"
                        && je.description.Contains($"StockMaster {id}"));

                if (journalEntry != null)
                {
                    var journalDetails = _context.JournalDetail
                        .Where(jd => jd.journalEntryId == journalEntry.Id);
                    var ledgerEntries = _context.Ledger
                        .Where(l => l.journalEntryId == journalEntry.Id);

                    _context.JournalDetail.RemoveRange(journalDetails);
                    _context.Ledger.RemoveRange(ledgerEntries);
                    _context.JournalEntry.Remove(journalEntry);
                }

                // Delete stock details
                var details = _context.StockDetail.Where(d => d.stockMasterId == id);
                _context.StockDetail.RemoveRange(details);

                // Delete stock master
                _context.StockMaster.Remove(purchaseReturn);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _notyf.Success("Purchase Return Voucher Deleted Successfully");
                return RedirectToAction("PurchaseReturnVoucher", new { activeTab = "list" });
            }
            catch (Exception ex)
            {
                _notyf.Error($"An Error Occurred: {ex.Message}");
                return RedirectToAction("PurchaseReturnVoucher", new { activeTab = "list" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, int page = 1, int pageSize = 5)
        {
            var purchaseReturn = await _context.StockMaster
                .Include(u => u.User)
                .Include(v => v.Vender)
                .Include(t => t.Transporter)
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (purchaseReturn == null)
            {
                _notyf.Error("Purchase Return not found");
                return RedirectToAction("PurchaseReturnVoucher");
            }

            var purchaseReturnDetail = await _context.StockDetail
                .Include(it => it.Item)
                .Include(w => w.Warehouse)
                .Where(d => d.stockMasterId == id)
                .ToListAsync();

            var model = new PurchaseViewModel
            {
                StockMaster = purchaseReturn,
                StockDetail = purchaseReturnDetail
            };

            int totalPurchase = await _context.StockMaster
                .CountAsync(d => d.etype == "PurchaseReturn");

            var purchaseReturnData = await _context.StockMaster
                .Where(j => j.etype == "PurchaseReturn")
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
            ViewBag.ActiveTab = "form";
            ViewBag.Warehouses = await _context.Warehouse.ToListAsync();
            ViewBag.Items = await _context.Item.ToListAsync();
            ViewBag.Venders = await _context.Vender.ToListAsync();
            ViewBag.Transporters = await _context.Transporter.ToListAsync();
            ViewBag.PurchaseReturn = purchaseReturnData;

            return View("~/Views/Purchase/PurchaseReturnVoucher.cshtml", model);
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