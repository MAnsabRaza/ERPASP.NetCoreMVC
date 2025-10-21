using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

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

        public async Task<IActionResult> PurchaseVoucher()
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
            ViewBag.Warehouses = await _context.Warehouse.ToListAsync();
            ViewBag.Items = await _context.Item.ToListAsync();
            ViewBag.Venders = await _context.Vender.ToListAsync();
            ViewBag.Transporters = await _context.Transporter.ToListAsync();
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
                    // Define Chart of Account IDs for Inventory and Accounts Payable
                    const int inventoryAccountId = 1; // ChartOfAccountId for Inventory
                    const int accountsPayableAccountId = 2; // ChartOfAccountId for Accounts Payable

                    if (pvm.StockMaster.Id > 0)
                    {
                        // Update existing purchase
                        var existingPurchase = await _context.StockMaster
                            .FirstOrDefaultAsync(x => x.Id == pvm.StockMaster.Id);
                        if (existingPurchase != null)
                        {
                            // Store old net_amount for vendor balance adjustment
                            decimal oldNetAmount = existingPurchase.net_amount;

                            // Update StockMaster
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
                            existingPurchase.net_amount = pvm.StockMaster.net_amount;
                            existingPurchase.remarks = pvm.StockMaster.remarks;
                            _context.Update(existingPurchase);

                            // Update vendor balance
                            var vendor = await _context.Vender
                                .FirstOrDefaultAsync(v => v.Id == pvm.StockMaster.venderId);
                            if (vendor != null)
                            {
                                vendor.current_balance = vendor.current_balance - oldNetAmount + pvm.StockMaster.net_amount;
                                _context.Update(vendor);
                            }

                            // Remove existing StockDetail
                            var existingDetails = _context.StockDetail
                                .Where(d => d.stockMasterId == existingPurchase.Id);
                            _context.StockDetail.RemoveRange(existingDetails);

                            // Remove existing JournalEntry, JournalDetail, and Ledger entries
                            var existingJournalEntry = await _context.JournalEntry
                                .FirstOrDefaultAsync(je => je.etype == "purchase" && je.description == $"Purchase Entry for StockMaster {existingPurchase.Id}");
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

                            // Create new JournalEntry
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

                            // Create JournalDetail entries
                            var journalDetails = new List<JournalDetail>
                            {
                                new JournalDetail
                                {
                                    current_date = pvm.StockMaster.current_date,
                                    journalEntryId = journalEntry.Id,
                                    chartOfAccountId = inventoryAccountId,
                                    debit_amount = pvm.StockMaster.net_amount,
                                    credit_amount = 0.00m,
                                    description = "Purchase Inventory"
                                },
                                new JournalDetail
                                {
                                    current_date = pvm.StockMaster.current_date,
                                    journalEntryId = journalEntry.Id,
                                    chartOfAccountId = accountsPayableAccountId,
                                    debit_amount = 0.00m,
                                    credit_amount = pvm.StockMaster.net_amount,
                                    description = "Purchase Liability"
                                }
                            };
                            _context.JournalDetail.AddRange(journalDetails);

                            // Update Ledger entries
                            var ledgerEntries = new List<Ledger>
                            {
                                new Ledger
                                {
                                    current_date = pvm.StockMaster.current_date,
                                    companyId = companyId,
                                    chartOfAccountId = inventoryAccountId,
                                    journalEntryId = journalEntry.Id,
                                    debit_amount = pvm.StockMaster.net_amount,
                                    credit_amount = 0.00m,
                                    running_balance = (await _context.Ledger
                                        .Where(l => l.chartOfAccountId == inventoryAccountId && l.companyId == companyId)
                                        .OrderByDescending(l => l.Id)
                                        .Select(l => l.running_balance)
                                        .FirstOrDefaultAsync()) + pvm.StockMaster.net_amount,
                                    description = "Purchase Inventory"
                                },
                                new Ledger
                                {
                                    current_date = pvm.StockMaster.current_date,
                                    companyId = companyId,
                                    chartOfAccountId = accountsPayableAccountId,
                                    journalEntryId = journalEntry.Id,
                                    debit_amount = 0.00m,
                                    credit_amount = pvm.StockMaster.net_amount,
                                    running_balance = (await _context.Ledger
                                        .Where(l => l.chartOfAccountId == accountsPayableAccountId && l.companyId == companyId)
                                        .OrderByDescending(l => l.Id)
                                        .Select(l => l.running_balance)
                                        .FirstOrDefaultAsync()) - pvm.StockMaster.net_amount,
                                    description = "Purchase Liability"
                                }
                            };
                            _context.Ledger.AddRange(ledgerEntries);

                            // Add new StockDetail
                            foreach (var d in pvm.StockDetail)
                            {
                                d.stockMasterId = existingPurchase.Id;
                                _context.StockDetail.Add(d);
                            }

                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();
                            _notyf.Success("Purchase Voucher Updated Successfully");
                        }
                    }
                    else
                    {
                        // Create new purchase
                        pvm.StockMaster.customerId = null;
                        _context.StockMaster.Add(pvm.StockMaster);
                        await _context.SaveChangesAsync();

                        // Update vendor balance
                        var vendor = await _context.Vender
                            .FirstOrDefaultAsync(v => v.Id == pvm.StockMaster.venderId);
                        if (vendor != null)
                        {
                            vendor.current_balance += pvm.StockMaster.net_amount;
                            _context.Update(vendor);
                        }

                        // Create JournalEntry
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

                        // Create JournalDetail entries
                        var journalDetails = new List<JournalDetail>
                        {
                            new JournalDetail
                            {
                                current_date = pvm.StockMaster.current_date,
                                journalEntryId = journalEntry.Id,
                                chartOfAccountId = inventoryAccountId,
                                debit_amount = pvm.StockMaster.net_amount,
                                credit_amount = 0.00m,
                                description = "Purchase Inventory"
                            },
                            new JournalDetail
                            {
                                current_date = pvm.StockMaster.current_date,
                                journalEntryId = journalEntry.Id,
                                chartOfAccountId = accountsPayableAccountId,
                                debit_amount = 0.00m,
                                credit_amount = pvm.StockMaster.net_amount,
                                description = "Purchase Liability"
                            }
                        };
                        _context.JournalDetail.AddRange(journalDetails);

                        // Create Ledger entries
                        var ledgerEntries = new List<Ledger>
                        {
                            new Ledger
                            {
                                current_date = pvm.StockMaster.current_date,
                                companyId = companyId,
                                chartOfAccountId = inventoryAccountId,
                                journalEntryId = journalEntry.Id,
                                debit_amount = pvm.StockMaster.net_amount,
                                credit_amount = 0.00m,
                                running_balance = (await _context.Ledger
                                    .Where(l => l.chartOfAccountId == inventoryAccountId && l.companyId == companyId)
                                    .OrderByDescending(l => l.Id)
                                    .Select(l => l.running_balance)
                                    .FirstOrDefaultAsync()) + pvm.StockMaster.net_amount,
                                description = "Purchase Inventory"
                            },
                            new Ledger
                            {
                                current_date = pvm.StockMaster.current_date,
                                companyId = companyId,
                                chartOfAccountId = accountsPayableAccountId,
                                journalEntryId = journalEntry.Id,
                                debit_amount = 0.00m,
                                credit_amount = pvm.StockMaster.net_amount,
                                running_balance = (await _context.Ledger
                                    .Where(l => l.chartOfAccountId == accountsPayableAccountId && l.companyId == companyId)
                                    .OrderByDescending(l => l.Id)
                                    .Select(l => l.running_balance)
                                    .FirstOrDefaultAsync()) - pvm.StockMaster.net_amount,
                                description = "Purchase Liability"
                            }
                        };
                        _context.Ledger.AddRange(ledgerEntries);

                        // Add StockDetail
                        foreach (var d in pvm.StockDetail)
                        {
                            d.stockMasterId = pvm.StockMaster.Id;
                            _context.StockDetail.Add(d);
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _notyf.Success("Purchase Voucher Created Successfully");
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
                    // Update vendor balance
                    var vendor = await _context.Vender
                        .FirstOrDefaultAsync(v => v.Id == purchase.venderId);
                    if (vendor != null)
                    {
                        vendor.current_balance -= purchase.net_amount;
                        _context.Update(vendor);
                    }

                    // Remove JournalEntry, JournalDetail, and Ledger entries
                    var journalEntry = await _context.JournalEntry
                        .FirstOrDefaultAsync(je => je.etype == "purchase" && je.description == $"Purchase Entry for StockMaster {id}");
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

                    // Remove StockDetail and StockMaster
                    var details = _context.StockDetail.Where(d => d.stockMasterId == id);
                    _context.StockDetail.RemoveRange(details);
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
    }
}