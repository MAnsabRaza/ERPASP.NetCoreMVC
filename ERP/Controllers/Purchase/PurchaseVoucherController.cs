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
        // PRIVATE HELPER: ViewBag data load
        // ════════════════════════════════════════════════════════════════
        private async Task LoadViewBagData(int page, int pageSize)
        {
            ViewBag.Warehouses = await _context.Warehouse.ToListAsync();
            ViewBag.Items = await _context.Item.ToListAsync();
            ViewBag.Venders = await _context.Vender.ToListAsync();
            ViewBag.Transporters = await _context.Transporter.ToListAsync();
            ViewBag.TaxSetup = await _context.TaxSetup
                .Where(t => t.status == true && (t.applicable_on == "Both" || t.applicable_on == "Purchase"))
                .ToListAsync();
            ViewBag.Banks = await _context.Bank.Where(b => b.status == true).ToListAsync();

            int totalPurchase = await _context.StockMaster.CountAsync(d => d.etype == "Purchase");

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
                    PaymentStatus = j.payment_status,
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
                .Where(l => l.chartOfAccountId == chartOfAccountId && l.companyId == companyId)
                .OrderByDescending(l => l.Id)
                .Select(l => l.running_balance)
                .FirstOrDefaultAsync();
        }

        // ════════════════════════════════════════════════════════════════
        // PRIVATE HELPER: PaymentVoucher find via JournalEntry description
        // PaymentVoucher mein stockMasterId nahi — journal_entryId se link
        // ════════════════════════════════════════════════════════════════
        private async Task<PaymentVoucher?> FindPaymentVoucherByStockMaster(int stockMasterId)
        {
            var payJournal = await _context.JournalEntry
                .FirstOrDefaultAsync(je => je.etype == "payment" &&
                    je.description == $"Payment Entry for StockMaster {stockMasterId}");

            if (payJournal == null) return null;

            return await _context.PaymentVoucher
                .FirstOrDefaultAsync(pv => pv.journal_entryId == payJournal.Id);
        }

        // ════════════════════════════════════════════════════════════════
        // GET: Purchase Voucher (new form + list)
        // ════════════════════════════════════════════════════════════════
        public async Task<IActionResult> PurchaseVoucher(
            int page = 1, int pageSize = 5, string activeTab = "form")
        {
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
                StockDetail = new List<StockDetail>(),
                PaymentVoucher = new PaymentVoucher
                {
                    current_date = DateOnly.FromDateTime(DateTime.Now),
                    voucher_date = DateOnly.FromDateTime(DateTime.Now)
                }
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

            var existingPayment = await FindPaymentVoucherByStockMaster(id)
                                  ?? new PaymentVoucher
                                  {
                                      current_date = purchase.current_date,
                                      voucher_date = purchase.current_date
                                  };

            var model = new PurchaseViewModel
            {
                StockMaster = purchase,
                StockDetail = purchaseDetail,
                PaymentVoucher = existingPayment
            };

            await LoadViewBagData(page, pageSize);
            ViewBag.ActiveTab = "form";
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
            catch (Exception ex) { return BadRequest(ex.Message); }
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
                pvm.PaymentVoucher ??= new PaymentVoucher();

                // Payment hai ya nahi
                bool hasPayment = !string.IsNullOrEmpty(pvm.PaymentVoucher.method)
                                  && pvm.PaymentVoucher.amount > 0;

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Chart of Accounts
                    var purchaseAccount = await _context.ChartOfAccount.FirstOrDefaultAsync(c => c.name == "Purchase Account");
                    var accountsPayableAccount = await _context.ChartOfAccount.FirstOrDefaultAsync(c => c.name == "Accounts Payable");
                    var cashChartAccount = await _context.ChartOfAccount.FirstOrDefaultAsync(c => c.name == "Cash");
                    var bankChartAccount = await _context.ChartOfAccount.FirstOrDefaultAsync(c => c.name == "Bank");

                    if (purchaseAccount == null || accountsPayableAccount == null)
                    {
                        await transaction.RollbackAsync();
                        _notyf.Error("Chart of Accounts not found. Please add 'Purchase Account' and 'Accounts Payable'.");
                        return RedirectToAction("PurchaseVoucher");
                    }

                    int purchaseAccountId = purchaseAccount.Id;
                    int accountsPayableId = accountsPayableAccount.Id;

                    // Fiscal Year
                    var currentFiscalYear = await _context.FinancialYear
                        .Where(fy => fy.companyId == companyId && fy.status.ToLower() == "open")
                        .OrderByDescending(fy => fy.Id)
                        .FirstOrDefaultAsync();

                    if (currentFiscalYear == null)
                    {
                        await transaction.RollbackAsync();
                        _notyf.Error("No open Fiscal Year found.");
                        return RedirectToAction("PurchaseVoucher");
                    }

                    int fiscalYearId = currentFiscalYear.Id;

                    if (pvm.StockMaster.current_date < currentFiscalYear.start_date ||
                        pvm.StockMaster.current_date > currentFiscalYear.end_date)
                    {
                        await transaction.RollbackAsync();
                        _notyf.Error($"Entry date {pvm.StockMaster.current_date:dd-MMM-yyyy} is outside the open Fiscal Year range " +
                                     $"({currentFiscalYear.start_date:dd-MMM-yyyy} to {currentFiscalYear.end_date:dd-MMM-yyyy}).");
                        return RedirectToAction("PurchaseVoucher");
                    }

                    // ════════════════════════════════════
                    // A) CREATE NEW  (Id == 0)
                    // ════════════════════════════════════
                    if (pvm.StockMaster.Id == 0)
                    {
                        pvm.StockMaster.customerId = null;
                        pvm.StockMaster.etype = "Purchase";
                        pvm.StockMaster.payment_status = hasPayment ? "Paid" : "Unpaid";
                        pvm.StockMaster.fiscalYearId = fiscalYearId;

                        _context.StockMaster.Add(pvm.StockMaster);
                        await _context.SaveChangesAsync();

                        if (pvm.StockMaster.Id <= 0)
                        {
                            await transaction.RollbackAsync();
                            _notyf.Error("Failed to generate Purchase Voucher ID.");
                            return RedirectToAction("PurchaseVoucher");
                        }

                        // StockDetail + Item qty update
                        foreach (var detail in pvm.StockDetail)
                        {
                            detail.StockMasterId = pvm.StockMaster.Id;
                            _context.StockDetail.Add(detail);

                            var item = await _context.Item.FirstOrDefaultAsync(i => i.Id == detail.itemId);
                            if (item != null)
                            {
                                item.qty += detail.qty;
                                item.purchase_rate = detail.rate;
                                item.rate = detail.rate;
                                _context.Update(item);
                            }
                        }

                        // Vendor balance update
                        var vendor = await _context.Vender.FirstOrDefaultAsync(v => v.Id == pvm.StockMaster.venderId);
                        if (vendor != null)
                        {
                            vendor.current_balance -= pvm.StockMaster.net_amount;
                            _context.Update(vendor);
                        }

                        // Purchase JournalEntry
                        var purchaseJournal = new JournalEntry
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
                            fiscalYearId = fiscalYearId
                        };
                        _context.JournalEntry.Add(purchaseJournal);
                        await _context.SaveChangesAsync();

                        if (purchaseJournal.Id <= 0)
                        {
                            await transaction.RollbackAsync();
                            _notyf.Error("Failed to generate Journal Entry ID.");
                            return RedirectToAction("PurchaseVoucher");
                        }

                        // Purchase JournalDetail
                        _context.JournalDetail.AddRange(new List<JournalDetail>
                        {
                            new JournalDetail
                            {
                                current_date     = pvm.StockMaster.current_date,
                                journalEntryId   = purchaseJournal.Id,
                                chartOfAccountId = purchaseAccountId,
                                debit_amount     = pvm.StockMaster.net_amount,
                                credit_amount    = 0.00m,
                                description      = "Purchase Amount"
                            },
                            new JournalDetail
                            {
                                current_date     = pvm.StockMaster.current_date,
                                journalEntryId   = purchaseJournal.Id,
                                chartOfAccountId = accountsPayableId,
                                debit_amount     = 0.00m,
                                credit_amount    = pvm.StockMaster.net_amount,
                                description      = $"Payable to Vendor - {vendor?.name}"
                            }
                        });

                        // Purchase Ledger
                        decimal purchaseRunning = await GetRunningBalance(purchaseAccountId, companyId);
                        decimal payableRunning = await GetRunningBalance(accountsPayableId, companyId);

                        _context.Ledger.AddRange(new List<Ledger>
                        {
                            new Ledger
                            {
                                current_date     = pvm.StockMaster.current_date,
                                companyId        = companyId,
                                chartOfAccountId = purchaseAccountId,
                                journalEntryId   = purchaseJournal.Id,
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
                                journalEntryId   = purchaseJournal.Id,
                                debit_amount     = 0.00m,
                                credit_amount    = pvm.StockMaster.net_amount,
                                running_balance  = payableRunning + pvm.StockMaster.net_amount,
                                description      = $"Payable to Vendor - {vendor?.name}"
                            }
                        });
                        await _context.SaveChangesAsync();

                        // Payment (agar diya hai)
                        if (hasPayment)
                        {
                            await SavePaymentVoucher(pvm, companyId, userId, fiscalYearId,
                                accountsPayableId, cashChartAccount, bankChartAccount, vendor);
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _notyf.Success("Purchase Voucher Created Successfully!");
                    }

                    // ════════════════════════════════════
                    // B) UPDATE EXISTING  (Id > 0)
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

                        // Old StockDetail → qty reverse
                        var oldDetails = await _context.StockDetail
                            .Where(d => d.StockMasterId == existingPurchase.Id).ToListAsync();
                        foreach (var od in oldDetails)
                        {
                            var oi = await _context.Item.FirstOrDefaultAsync(i => i.Id == od.itemId);
                            if (oi != null) { oi.qty -= od.qty; _context.Update(oi); }
                        }
                        _context.StockDetail.RemoveRange(oldDetails);

                        // StockMaster update
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
                        existingPurchase.payment_status = hasPayment ? "Paid" : "Unpaid";
                        existingPurchase.fiscalYearId = fiscalYearId;
                        _context.Update(existingPurchase);

                        // Vendor balance reverse/re-apply
                        var vendor = await _context.Vender.FirstOrDefaultAsync(v => v.Id == pvm.StockMaster.venderId);
                        if (vendor != null)
                        {
                            vendor.current_balance = vendor.current_balance + oldNetAmount - pvm.StockMaster.net_amount;
                            _context.Update(vendor);
                        }

                        // New item qty
                        foreach (var nd in pvm.StockDetail)
                        {
                            var ni = await _context.Item.FirstOrDefaultAsync(i => i.Id == nd.itemId);
                            if (ni != null)
                            {
                                ni.qty += nd.qty;
                                ni.purchase_rate = nd.rate;
                                ni.rate = nd.rate;
                                _context.Update(ni);
                            }
                        }

                        // New StockDetail
                        foreach (var detail in pvm.StockDetail)
                        {
                            detail.StockMasterId = existingPurchase.Id;
                            _context.StockDetail.Add(detail);
                        }

                        // Old Purchase Journal delete
                        var oldPurchaseJournal = await _context.JournalEntry
                            .FirstOrDefaultAsync(je => je.etype == "purchase" &&
                                je.description == $"Purchase Entry for StockMaster {existingPurchase.Id}");
                        if (oldPurchaseJournal != null)
                        {
                            _context.JournalDetail.RemoveRange(_context.JournalDetail.Where(jd => jd.journalEntryId == oldPurchaseJournal.Id));
                            _context.Ledger.RemoveRange(_context.Ledger.Where(l => l.journalEntryId == oldPurchaseJournal.Id));
                            _context.JournalEntry.Remove(oldPurchaseJournal);
                        }

                        // Old Payment Journal + PaymentVoucher delete
                        var oldPayJournal = await _context.JournalEntry
                            .FirstOrDefaultAsync(je => je.etype == "payment" &&
                                je.description == $"Payment Entry for StockMaster {existingPurchase.Id}");
                        if (oldPayJournal != null)
                        {
                            var oldPv = await _context.PaymentVoucher
                                .FirstOrDefaultAsync(pv => pv.journal_entryId == oldPayJournal.Id);
                            if (oldPv != null) _context.PaymentVoucher.Remove(oldPv);

                            _context.JournalDetail.RemoveRange(_context.JournalDetail.Where(jd => jd.journalEntryId == oldPayJournal.Id));
                            _context.Ledger.RemoveRange(_context.Ledger.Where(l => l.journalEntryId == oldPayJournal.Id));
                            _context.JournalEntry.Remove(oldPayJournal);
                        }

                        await _context.SaveChangesAsync();

                        // New Purchase JournalEntry
                        var purchaseJournal = new JournalEntry
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
                            fiscalYearId = fiscalYearId
                        };
                        _context.JournalEntry.Add(purchaseJournal);
                        await _context.SaveChangesAsync();

                        if (purchaseJournal.Id <= 0)
                        {
                            await transaction.RollbackAsync();
                            _notyf.Error("Failed to generate Journal Entry ID.");
                            return RedirectToAction("PurchaseVoucher");
                        }

                        // New Purchase JournalDetail
                        _context.JournalDetail.AddRange(new List<JournalDetail>
                        {
                            new JournalDetail
                            {
                                current_date     = pvm.StockMaster.current_date,
                                journalEntryId   = purchaseJournal.Id,
                                chartOfAccountId = purchaseAccountId,
                                debit_amount     = pvm.StockMaster.net_amount,
                                credit_amount    = 0.00m,
                                description      = "Purchase Amount"
                            },
                            new JournalDetail
                            {
                                current_date     = pvm.StockMaster.current_date,
                                journalEntryId   = purchaseJournal.Id,
                                chartOfAccountId = accountsPayableId,
                                debit_amount     = 0.00m,
                                credit_amount    = pvm.StockMaster.net_amount,
                                description      = $"Payable to Vendor - {vendor?.name}"
                            }
                        });

                        // New Purchase Ledger
                        decimal purchaseRunning = await GetRunningBalance(purchaseAccountId, companyId);
                        decimal payableRunning = await GetRunningBalance(accountsPayableId, companyId);

                        _context.Ledger.AddRange(new List<Ledger>
                        {
                            new Ledger
                            {
                                current_date     = pvm.StockMaster.current_date,
                                companyId        = companyId,
                                chartOfAccountId = purchaseAccountId,
                                journalEntryId   = purchaseJournal.Id,
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
                                journalEntryId   = purchaseJournal.Id,
                                debit_amount     = 0.00m,
                                credit_amount    = pvm.StockMaster.net_amount,
                                running_balance  = payableRunning + pvm.StockMaster.net_amount,
                                description      = $"Payable to Vendor - {vendor?.name}"
                            }
                        });
                        await _context.SaveChangesAsync();

                        // New Payment
                        if (hasPayment)
                        {
                            pvm.StockMaster.Id = existingPurchase.Id;
                            await SavePaymentVoucher(pvm, companyId, userId, fiscalYearId,
                                accountsPayableId, cashChartAccount, bankChartAccount, vendor);
                        }

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
        // PRIVATE HELPER: PaymentVoucher Save
        //
        // PaymentVoucher model fields used:
        //   current_date  = StockMaster.current_date
        //   voucher_date  = StockMaster.due_date
        //   amount        = form se
        //   method        = Cash / Cheque / Bank
        //   status        = true (Paid)
        //   payment_type  = "Purchase"
        //   cheque_no     = form se (Cheque only)
        //   cheque_date   = form se (Cheque only)
        //   journal_entryId = payment JournalEntry ka Id (required, non-nullable)
        //   companyId     = session
        //   venderId      = StockMaster.venderId (jo vendor select kiya)
        //   customerId    = null
        //   bankAccountId = form se (Bank/Cheque only)
        // ════════════════════════════════════════════════════════════════
        private async Task SavePaymentVoucher(
            PurchaseViewModel pvm,
            int companyId,
            int userId,
            int fiscalYearId,
            int accountsPayableId,
            ChartOfAccount? cashChartAccount,
            ChartOfAccount? bankChartAccount,
            Vender? vendor)
        {
            var pv = pvm.PaymentVoucher;

            // Payment ke liye Chart of Account determine karo
            int paymentChartId = 0;
            string paymentChartName = "";

            if (pv.method == "Cash" && cashChartAccount != null)
            {
                paymentChartId = cashChartAccount.Id;
                paymentChartName = "Cash";
            }
            else if ((pv.method == "Bank" || pv.method == "Cheque") && bankChartAccount != null)
            {
                paymentChartId = bankChartAccount.Id;
                paymentChartName = pv.method == "Cheque"
                    ? $"Bank (Cheque #{pv.cheque_no})"
                    : "Bank Transfer";
            }

            // ── STEP 1: Payment JournalEntry pehle save karo (Id generate hogi) ──
            var payJournal = new JournalEntry
            {
                current_date = pvm.StockMaster.current_date,
                due_date = pvm.StockMaster.due_date,
                posted_date = pvm.StockMaster.posted_date,
                venderId = pvm.StockMaster.venderId,  // vendor ka ID jo select kiya
                companyId = companyId,
                userId = userId,
                etype = "payment",
                description = $"Payment Entry for StockMaster {pvm.StockMaster.Id}",
                total_debit = pv.amount,
                total_credit = pv.amount,
                fiscalYearId = fiscalYearId
            };
            _context.JournalEntry.Add(payJournal);
            await _context.SaveChangesAsync(); // Id yahan generate hogi

            // ── STEP 2: PaymentVoucher save karo ──
            // journal_entryId required hai isliye STEP 1 ke baad save karo
            var newPv = new PaymentVoucher
            {
                current_date = pvm.StockMaster.current_date,  // voucher ki current date
                voucher_date = pvm.StockMaster.due_date,       // due date = voucher date
                amount = pv.amount,
                method = pv.method,                       // Cash / Cheque / Bank
                status = true,                            // default: Paid
                payment_type = "Purchase",
                cheque_no = pv.method == "Cheque" ? pv.cheque_no : null,
                cheque_date = pv.method == "Cheque" ? pv.cheque_date : null,
                journal_entryId = payJournal.Id,                  // required — STEP 1 ke baad guaranteed
                companyId = companyId,                       // session company
                venderId = pvm.StockMaster.venderId,       // selected vendor ka Id
                customerId = null,
                bankAccountId = (pv.method == "Bank" || pv.method == "Cheque")
                                  ? pv.bankAccountId
                                  : null                           // Cash mein null
            };
            _context.PaymentVoucher.Add(newPv);
            await _context.SaveChangesAsync();

            // ── STEP 3: JournalDetail + Ledger (sirf valid paymentChartId par) ──
            if (paymentChartId > 0)
            {
                // JournalDetail:
                //   Accounts Payable → DEBIT (liability ghati, hum ne vendor ko pay kiya)
                //   Cash / Bank      → CREDIT (asset ghata, paise bahar gaye)
                _context.JournalDetail.AddRange(new List<JournalDetail>
                {
                    new JournalDetail
                    {
                        current_date     = pvm.StockMaster.current_date,
                        journalEntryId   = payJournal.Id,
                        chartOfAccountId = accountsPayableId,
                        debit_amount     = pv.amount,
                        credit_amount    = 0.00m,
                        description      = $"Payment to Vendor - {vendor?.name}"
                    },
                    new JournalDetail
                    {
                        current_date     = pvm.StockMaster.current_date,
                        journalEntryId   = payJournal.Id,
                        chartOfAccountId = paymentChartId,
                        debit_amount     = 0.00m,
                        credit_amount    = pv.amount,
                        description      = $"Paid via {paymentChartName}"
                    }
                });

                // Ledger:
                //   AP running balance ghata (debit side)
                //   Cash/Bank running balance badha (credit side — cash gaya bahar)
                decimal payableRunning = await GetRunningBalance(accountsPayableId, companyId);
                decimal paymentRunning = await GetRunningBalance(paymentChartId, companyId);

                _context.Ledger.AddRange(new List<Ledger>
                {
                    new Ledger
                    {
                        current_date     = pvm.StockMaster.current_date,
                        companyId        = companyId,
                        chartOfAccountId = accountsPayableId,
                        journalEntryId   = payJournal.Id,
                        debit_amount     = pv.amount,
                        credit_amount    = 0.00m,
                        running_balance  = payableRunning - pv.amount,
                        description      = $"Payment to Vendor - {vendor?.name}"
                    },
                    new Ledger
                    {
                        current_date     = pvm.StockMaster.current_date,
                        companyId        = companyId,
                        chartOfAccountId = paymentChartId,
                        journalEntryId   = payJournal.Id,
                        debit_amount     = 0.00m,
                        credit_amount    = pv.amount,
                        running_balance  = paymentRunning + pv.amount,
                        description      = $"Paid via {paymentChartName}"
                    }
                });

                await _context.SaveChangesAsync();
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

                // Vendor balance reverse
                var vendor = await _context.Vender.FirstOrDefaultAsync(v => v.Id == purchase.venderId);
                if (vendor != null) { vendor.current_balance += purchase.net_amount; _context.Update(vendor); }

                // StockDetail → item qty reverse
                var details = await _context.StockDetail.Where(d => d.StockMasterId == id).ToListAsync();
                foreach (var detail in details)
                {
                    var item = await _context.Item.FirstOrDefaultAsync(i => i.Id == detail.itemId);
                    if (item != null) { item.qty -= detail.qty; _context.Update(item); }
                }
                _context.StockDetail.RemoveRange(details);

                // Payment Journal → PaymentVoucher → JournalDetail → Ledger delete
                var payJournal = await _context.JournalEntry
                    .FirstOrDefaultAsync(je => je.etype == "payment" &&
                        je.description == $"Payment Entry for StockMaster {id}");
                if (payJournal != null)
                {
                    var paymentVoucher = await _context.PaymentVoucher
                        .FirstOrDefaultAsync(pv => pv.journal_entryId == payJournal.Id);
                    if (paymentVoucher != null) _context.PaymentVoucher.Remove(paymentVoucher);

                    _context.JournalDetail.RemoveRange(_context.JournalDetail.Where(jd => jd.journalEntryId == payJournal.Id));
                    _context.Ledger.RemoveRange(_context.Ledger.Where(l => l.journalEntryId == payJournal.Id));
                    _context.JournalEntry.Remove(payJournal);
                }

                // Purchase Journal → JournalDetail → Ledger delete
                var purchaseJournal = await _context.JournalEntry
                    .FirstOrDefaultAsync(je => je.etype == "purchase" &&
                        je.description == $"Purchase Entry for StockMaster {id}");
                if (purchaseJournal != null)
                {
                    _context.JournalDetail.RemoveRange(_context.JournalDetail.Where(jd => jd.journalEntryId == purchaseJournal.Id));
                    _context.Ledger.RemoveRange(_context.Ledger.Where(l => l.journalEntryId == purchaseJournal.Id));
                    _context.JournalEntry.Remove(purchaseJournal);
                }

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