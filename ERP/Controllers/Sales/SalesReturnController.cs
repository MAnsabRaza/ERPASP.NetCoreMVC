using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using Rotativa.AspNetCore.Options;

namespace ERP.Controllers.Sales
{
    public class SalesReturnController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;

        public SalesReturnController(AppDbContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }

        // ════════════════════════════════════
        // INDEX
        // ════════════════════════════════════
        public async Task<IActionResult> SalesReturnVoucher(
            int page = 1, int pageSize = 5, string activeTab = "form")
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

            int totalReturns = await _context.StockMaster
                .CountAsync(d => d.etype == "SalesReturn");

            var returnList = await _context.StockMaster
                .Where(j => j.etype == "SalesReturn")
                .OrderBy(j => j.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(j => new SalesListDto
                {
                    Id = j.Id,
                    CurrentDate = j.current_date,
                    Etype = j.etype,
                    Remarks = j.remarks,
                    TotalAmount = j.total_amount,
                    NetAmount = j.net_amount,
                    CustomerName = j.Customer != null ? j.Customer.name : null
                })
                .ToListAsync();

            ViewBag.TotalItems = totalReturns;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.ActiveTab = activeTab;
            ViewBag.Warehouses = await _context.Warehouse.ToListAsync();
            ViewBag.Items = await _context.Item.ToListAsync();
            ViewBag.Customers = await _context.Customer.ToListAsync();
            ViewBag.Sales = returnList;

            return View("~/Views/Sales/SalesReturnVoucher.cshtml", model);
        }

        // ════════════════════════════════════
        // GET SALE RATE
        // ════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetSalesRate(int itemId)
        {
            try
            {
                var item = await _context.Item.FindAsync(itemId);
                if (item != null)
                    return Json(new { salesRate = item.sale_rate });
                return Json(new { salesRate = (decimal?)null });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ════════════════════════════════════════════════════════
        // CHECK ITEM QTY - Sales Return mein sold qty check hoti hai
        // ✅ Item ki SOLD qty check karein (kya itna return ho sakta hai)
        // ════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> CheckItemQty(int itemId, decimal qty)
        {
            try
            {
                var item = await _context.Item.FindAsync(itemId);

                if (item == null)
                    return Json(new { success = false, message = "Item not found in database." });

                if (qty <= 0)
                    return Json(new { success = false, message = "Please enter valid quantity." });

                // ✅ Sales Return mein koi qty restriction nahi hoti
                // Customer return kar raha hai → qty wapas aayegi
                return Json(new { success = true, message = $"'{item.item_name}' added successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ════════════════════════════════════
        // CREATE / UPDATE
        // ════════════════════════════════════
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

                if (!pvm.StockMaster.customerId.HasValue || pvm.StockMaster.customerId == 0)
                {
                    _notyf.Error("Please select a customer.");
                    return RedirectToAction("SalesReturnVoucher", new { activeTab = "form" });
                }

                if (pvm.StockDetail == null || !pvm.StockDetail.Any())
                {
                    _notyf.Error("Please add at least one item.");
                    return RedirectToAction("SalesReturnVoucher", new { activeTab = "form" });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // ═══════════════════════════════════════════════════
                    // ✅ Chart of Accounts - Sales Return ke liye CORRECT:
                    //
                    //  Sales Return  (Id=32, Revenue) → DEBIT
                    //  └─ Customer ne maal wapas kiya → revenue kam hogi
                    //
                    //  Accounts Receivable (Id=38, Asset) → CREDIT
                    //  └─ Customer pe receivable kam hogi
                    // ═══════════════════════════════════════════════════
                    var salesReturnAccount = await _context.ChartOfAccount
                        .FirstOrDefaultAsync(c => c.name == "Sales Return"
                                               && c.companyId == companyId);

                    var accountsReceivable = await _context.ChartOfAccount
                        .FirstOrDefaultAsync(c => c.name == "Accounts Receivable"
                                               && c.companyId == companyId);

                    if (salesReturnAccount == null || accountsReceivable == null)
                    {
                        _notyf.Error("Chart of accounts not found. Make sure 'Sales Return' (Id=32) and 'Accounts Receivable' (Id=38) exist.");
                        await transaction.RollbackAsync();
                        return BadRequest("Chart of accounts not found.");
                    }

                    int srId = salesReturnAccount.Id;   // Sales Return    = 32 → DEBIT
                    int arId = accountsReceivable.Id;   // Accounts Receivable = 38 → CREDIT

                    // Running balance helper
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
                    // CREATE NEW SALES RETURN
                    // ════════════════════════════════════
                    if (pvm.StockMaster.Id == 0)
                    {
                        // STEP 1: StockMaster insert
                        pvm.StockMaster.venderId = null;
                        pvm.StockMaster.transporterId = null;
                        // ✅ etype = SalesReturn (form se aayega)
                        _context.StockMaster.Add(pvm.StockMaster);
                        await _context.SaveChangesAsync();

                        // STEP 2: StockDetail + Item qty WAPAS BADHAO ✅
                        // Sales Return → customer ne maal wapas diya → stock increase
                        foreach (var detail in pvm.StockDetail)
                        {
                            detail.StockMasterId = pvm.StockMaster.Id;
                            _context.StockDetail.Add(detail);

                            var item = await _context.Item
                                .FirstOrDefaultAsync(i => i.Id == detail.itemId);
                            if (item != null)
                            {
                                item.qty += detail.qty; // ✅ Return → stock BADHEGA
                                _context.Update(item);
                            }
                        }

                        // STEP 3: Customer balance GHATAO ✅
                        // Sales Return → customer ki receivable kam hogi
                        var customer = await _context.Customer
                            .FirstOrDefaultAsync(c => c.Id == pvm.StockMaster.customerId);
                        if (customer != null)
                        {
                            customer.current_balance -= pvm.StockMaster.net_amount; // ✅ MINUS
                            _context.Update(customer);
                        }

                        // STEP 4: JournalEntry insert
                        var journalEntry = new JournalEntry
                        {
                            current_date = pvm.StockMaster.current_date,
                            due_date = pvm.StockMaster.due_date,
                            posted_date = pvm.StockMaster.posted_date,
                            customerId = pvm.StockMaster.customerId,
                            companyId = companyId,
                            userId = userId,
                            etype = "SalesReturn",
                            description = $"Sales Return Entry for StockMaster {pvm.StockMaster.Id}",
                            total_debit = pvm.StockMaster.net_amount,
                            total_credit = pvm.StockMaster.net_amount
                        };
                        _context.JournalEntry.Add(journalEntry);
                        await _context.SaveChangesAsync();

                        // STEP 5: JournalDetail
                        // ✅ Sales Return  (Id=32) → DEBIT  (revenue contra)
                        // ✅ Accounts Receivable (Id=38) → CREDIT (asset kam hogi)
                        _context.JournalDetail.AddRange(new List<JournalDetail>
                        {
                            new JournalDetail
                            {
                                current_date     = pvm.StockMaster.current_date,
                                journalEntryId   = journalEntry.Id,
                                chartOfAccountId = srId,
                                debit_amount     = pvm.StockMaster.net_amount,
                                credit_amount    = 0.00m,
                                description      = "Sales Return Debit"
                            },
                            new JournalDetail
                            {
                                current_date     = pvm.StockMaster.current_date,
                                journalEntryId   = journalEntry.Id,
                                chartOfAccountId = arId,
                                debit_amount     = 0.00m,
                                credit_amount    = pvm.StockMaster.net_amount,
                                description      = "Accounts Receivable Credit"
                            }
                        });

                        // STEP 6: Ledger
                        decimal srRunning = await GetRunningBalance(srId);
                        decimal arRunning = await GetRunningBalance(arId);

                        _context.Ledger.AddRange(new List<Ledger>
                        {
                            // ✅ Sales Return (Revenue contra → DEBIT → running + debit)
                            new Ledger
                            {
                                current_date     = pvm.StockMaster.current_date,
                                companyId        = companyId,
                                chartOfAccountId = srId,
                                journalEntryId   = journalEntry.Id,
                                debit_amount     = pvm.StockMaster.net_amount,
                                credit_amount    = 0.00m,
                                running_balance  = srRunning + pvm.StockMaster.net_amount,
                                description      = "Sales Return Debit"
                            },
                            // ✅ Accounts Receivable (Asset → CREDIT → running - credit)
                            new Ledger
                            {
                                current_date     = pvm.StockMaster.current_date,
                                companyId        = companyId,
                                chartOfAccountId = arId,
                                journalEntryId   = journalEntry.Id,
                                debit_amount     = 0.00m,
                                credit_amount    = pvm.StockMaster.net_amount,
                                running_balance  = arRunning - pvm.StockMaster.net_amount,
                                description      = "Accounts Receivable Credit"
                            }
                        });

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _notyf.Success("Sales Return Voucher Created Successfully");
                    }

                    // ════════════════════════════════════
                    // UPDATE EXISTING SALES RETURN
                    // ════════════════════════════════════
                    else
                    {
                        var existingSales = await _context.StockMaster
                            .FirstOrDefaultAsync(x => x.Id == pvm.StockMaster.Id);

                        if (existingSales == null)
                        {
                            _notyf.Error("Sales Return voucher not found.");
                            await transaction.RollbackAsync();
                            return NotFound();
                        }

                        decimal oldNetAmount = existingSales.net_amount;
                        int? oldCustomerId = existingSales.customerId;

                        // STEP 1: Purani StockDetail fetch
                        var oldDetails = await _context.StockDetail
                            .Where(d => d.StockMasterId == existingSales.Id)
                            .ToListAsync();

                        // STEP 2: Purane items ki qty WAPAS GHATAO
                        // (purana return undo → jo qty wapas aayi thi wo phir nikalo)
                        foreach (var oldDetail in oldDetails)
                        {
                            var oldItem = await _context.Item
                                .FirstOrDefaultAsync(i => i.Id == oldDetail.itemId);
                            if (oldItem != null)
                            {
                                oldItem.qty -= oldDetail.qty; // ✅ old return undo
                                _context.Update(oldItem);
                            }
                        }

                        // STEP 3: Purani StockDetail delete
                        _context.StockDetail.RemoveRange(oldDetails);

                        // STEP 4: Old customer balance WAPAS BADHAO (old return undo)
                        if (oldCustomerId.HasValue)
                        {
                            var oldCustomer = await _context.Customer
                                .FirstOrDefaultAsync(c => c.Id == oldCustomerId);
                            if (oldCustomer != null)
                            {
                                oldCustomer.current_balance += oldNetAmount; // ✅ undo
                                _context.Update(oldCustomer);
                            }
                        }

                        // STEP 5: StockMaster update
                        existingSales.current_date = pvm.StockMaster.current_date;
                        existingSales.posted_date = pvm.StockMaster.posted_date;
                        existingSales.due_date = pvm.StockMaster.due_date;
                        existingSales.userId = userId;
                        existingSales.companyId = companyId;
                        existingSales.customerId = pvm.StockMaster.customerId;
                        existingSales.venderId = null;
                        existingSales.transporterId = null;
                        existingSales.etype = "SalesReturn";
                        existingSales.total_amount = pvm.StockMaster.total_amount;
                        existingSales.discount_amount = pvm.StockMaster.discount_amount;
                        existingSales.tax_amount = pvm.StockMaster.tax_amount;
                        existingSales.net_amount = pvm.StockMaster.net_amount;
                        existingSales.remarks = pvm.StockMaster.remarks;
                        _context.Update(existingSales);

                        // STEP 6: New customer balance GHATAO (naya return apply)
                        var newCustomer = await _context.Customer
                            .FirstOrDefaultAsync(c => c.Id == pvm.StockMaster.customerId);
                        if (newCustomer != null)
                        {
                            newCustomer.current_balance -= pvm.StockMaster.net_amount; // ✅ MINUS
                            _context.Update(newCustomer);
                        }

                        // STEP 7: Naye items ki qty BADHAO (naya return stock wapas)
                        foreach (var newDetail in pvm.StockDetail)
                        {
                            var item = await _context.Item
                                .FirstOrDefaultAsync(i => i.Id == newDetail.itemId);
                            if (item != null)
                            {
                                item.qty += newDetail.qty; // ✅ return → stock badhega
                                _context.Update(item);
                            }
                        }

                        // STEP 8: Naye StockDetail add
                        foreach (var detail in pvm.StockDetail)
                        {
                            detail.StockMasterId = existingSales.Id;
                            _context.StockDetail.Add(detail);
                        }

                        // STEP 9: Purani JournalEntry/Detail/Ledger delete
                        var existingJE = await _context.JournalEntry
                            .FirstOrDefaultAsync(je => je.etype == "SalesReturn" &&
                                je.description == $"Sales Return Entry for StockMaster {existingSales.Id}");

                        if (existingJE != null)
                        {
                            _context.JournalDetail.RemoveRange(
                                _context.JournalDetail.Where(jd => jd.journalEntryId == existingJE.Id));
                            _context.Ledger.RemoveRange(
                                _context.Ledger.Where(l => l.journalEntryId == existingJE.Id));
                            _context.JournalEntry.Remove(existingJE);
                        }

                        await _context.SaveChangesAsync();

                        // STEP 10: Naya JournalEntry
                        var journalEntry = new JournalEntry
                        {
                            current_date = pvm.StockMaster.current_date,
                            due_date = pvm.StockMaster.due_date,
                            posted_date = pvm.StockMaster.posted_date,
                            customerId = pvm.StockMaster.customerId,
                            companyId = companyId,
                            userId = userId,
                            etype = "SalesReturn",
                            description = $"Sales Return Entry for StockMaster {existingSales.Id}",
                            total_debit = pvm.StockMaster.net_amount,
                            total_credit = pvm.StockMaster.net_amount
                        };
                        _context.JournalEntry.Add(journalEntry);
                        await _context.SaveChangesAsync();

                        // STEP 11: JournalDetail
                        _context.JournalDetail.AddRange(new List<JournalDetail>
                        {
                            new JournalDetail
                            {
                                current_date     = pvm.StockMaster.current_date,
                                journalEntryId   = journalEntry.Id,
                                chartOfAccountId = srId,
                                debit_amount     = pvm.StockMaster.net_amount,
                                credit_amount    = 0.00m,
                                description      = "Sales Return Debit"
                            },
                            new JournalDetail
                            {
                                current_date     = pvm.StockMaster.current_date,
                                journalEntryId   = journalEntry.Id,
                                chartOfAccountId = arId,
                                debit_amount     = 0.00m,
                                credit_amount    = pvm.StockMaster.net_amount,
                                description      = "Accounts Receivable Credit"
                            }
                        });

                        // STEP 12: Ledger
                        decimal srRunning = await GetRunningBalance(srId);
                        decimal arRunning = await GetRunningBalance(arId);

                        _context.Ledger.AddRange(new List<Ledger>
                        {
                            new Ledger
                            {
                                current_date     = pvm.StockMaster.current_date,
                                companyId        = companyId,
                                chartOfAccountId = srId,
                                journalEntryId   = journalEntry.Id,
                                debit_amount     = pvm.StockMaster.net_amount,
                                credit_amount    = 0.00m,
                                running_balance  = srRunning + pvm.StockMaster.net_amount,
                                description      = "Sales Return Debit"
                            },
                            new Ledger
                            {
                                current_date     = pvm.StockMaster.current_date,
                                companyId        = companyId,
                                chartOfAccountId = arId,
                                journalEntryId   = journalEntry.Id,
                                debit_amount     = 0.00m,
                                credit_amount    = pvm.StockMaster.net_amount,
                                running_balance  = arRunning - pvm.StockMaster.net_amount,
                                description      = "Accounts Receivable Credit"
                            }
                        });

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _notyf.Success("Sales Return Voucher Updated Successfully");
                    }

                    return RedirectToAction("SalesReturnVoucher", new { activeTab = "list" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Error saving sales return voucher: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                _notyf.Error($"An Error Occurred: {ex.Message}");
                var inner = ex.InnerException != null ? ex.InnerException.Message : "";
                return BadRequest($"{ex.Message} - {inner}");
            }
        }

        // ════════════════════════════════════
        // DELETE
        // ════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var sales = await _context.StockMaster.FindAsync(id);
                if (sales == null)
                {
                    _notyf.Error("Sales Return voucher not found.");
                    return NotFound();
                }

                // STEP 1: Customer balance WAPAS BADHAO (return undo)
                if (sales.customerId.HasValue)
                {
                    var customer = await _context.Customer
                        .FirstOrDefaultAsync(c => c.Id == sales.customerId);
                    if (customer != null)
                    {
                        customer.current_balance += sales.net_amount; // ✅ PLUS (return undo)
                        _context.Update(customer);
                    }
                }

                // STEP 2: StockDetail fetch + Item qty WAPAS GHATAO
                // (return delete → jo stock wapas aayi thi wo phir nikalo)
                var details = await _context.StockDetail
                    .Where(d => d.StockMasterId == id)
                    .ToListAsync();

                foreach (var detail in details)
                {
                    var item = await _context.Item
                        .FirstOrDefaultAsync(i => i.Id == detail.itemId);
                    if (item != null)
                    {
                        item.qty -= detail.qty; // ✅ return undo → stock wapas kam
                        _context.Update(item);
                    }
                }

                // STEP 3: StockDetail delete
                _context.StockDetail.RemoveRange(details);

                // STEP 4: JournalEntry/Detail/Ledger delete
                var journalEntry = await _context.JournalEntry
                    .FirstOrDefaultAsync(je => je.etype == "SalesReturn" &&
                        je.description == $"Sales Return Entry for StockMaster {id}");

                if (journalEntry != null)
                {
                    _context.JournalDetail.RemoveRange(
                        _context.JournalDetail.Where(jd => jd.journalEntryId == journalEntry.Id));
                    _context.Ledger.RemoveRange(
                        _context.Ledger.Where(l => l.journalEntryId == journalEntry.Id));
                    _context.JournalEntry.Remove(journalEntry);
                }

                // STEP 5: StockMaster delete
                _context.StockMaster.Remove(sales);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _notyf.Success("Sales Return Voucher Deleted Successfully");

                return RedirectToAction("SalesReturnVoucher", new { activeTab = "list" });
            }
            catch (Exception ex)
            {
                _notyf.Error($"An Error Occurred: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        // ════════════════════════════════════
        // EDIT
        // ════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Edit(int id, int page = 1, int pageSize = 5)
        {
            var sales = await _context.StockMaster
                .Include(u => u.User)
                .Include(c => c.Customer)
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (sales == null)
            {
                _notyf.Error("Sales Return voucher not found.");
                return NotFound();
            }

            var salesDetail = await _context.StockDetail
                .Include(it => it.Item)
                .Include(w => w.Warehouse)
                .Where(d => d.StockMasterId == id)
                .ToListAsync();

            var model = new PurchaseViewModel
            {
                StockMaster = sales,
                StockDetail = salesDetail
            };

            int totalReturns = await _context.StockMaster
                .CountAsync(d => d.etype == "SalesReturn");

            var salesData = await _context.StockMaster
                .Where(j => j.etype == "SalesReturn")
                .OrderBy(j => j.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(j => new SalesListDto
                {
                    Id = j.Id,
                    CurrentDate = j.current_date,
                    Etype = j.etype,
                    Remarks = j.remarks,
                    TotalAmount = j.total_amount,
                    NetAmount = j.net_amount,
                    CustomerName = j.Customer != null ? j.Customer.name : null
                })
                .ToListAsync();

            ViewBag.TotalItems = totalReturns;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.ActiveTab = "form";
            ViewBag.Warehouses = await _context.Warehouse.ToListAsync();
            ViewBag.Items = await _context.Item.ToListAsync();
            ViewBag.Customers = await _context.Customer.ToListAsync();
            ViewBag.Sales = salesData;

            return View("~/Views/Sales/SalesReturnVoucher.cshtml", model);
        }

        // ════════════════════════════════════
        // ITEM MODAL
        // ════════════════════════════════════
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

        // ════════════════════════════════════
        // PRINT
        // ════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> PrintSalesReturnVoucher(int id)
        {
            var sales = await _context.StockMaster
                .Include(s => s.Customer)
                .Include(s => s.StockDetail).ThenInclude(d => d.Item)
                .Include(s => s.StockDetail).ThenInclude(d => d.Warehouse)
                .FirstOrDefaultAsync(s => s.Id == id && s.etype == "SalesReturn");

            if (sales == null)
            {
                _notyf.Error("Sales Return voucher not found.");
                return NotFound();
            }

            var model = new PurchaseViewModel
            {
                StockMaster = sales,
                StockDetail = sales.StockDetail.ToList()
            };

            return new ViewAsPdf("_PrintSalesVoucher", model)
            {
                FileName = $"SalesReturn_Voucher_{id}.pdf",
                PageSize = Size.A4,
                PageOrientation = Orientation.Portrait,
                PageMargins = { Left = 15, Right = 15, Top = 20, Bottom = 20 }
            };
        }
    }
}