using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using Rotativa.AspNetCore.Options;

namespace ERP.Controllers.Sales
{
    public class SalesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;

        public SalesController(AppDbContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }

        public async Task<IActionResult> SalesVoucher(int page = 1, int pageSize = 5, string activeTab = "form")
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

            int totalSales = await _context.StockMaster
                .CountAsync(d => d.etype == "Sales");

            var salesDetail = await _context.StockMaster
                .Where(j => j.etype == "Sales")
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
                    CustomerName = j.Customer != null ? j.Customer.name : null,
                })
                .ToListAsync();

            // Pass pagination info
            ViewBag.TotalItems = totalSales;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.ActiveTab = activeTab;

            // Dropdowns
            ViewBag.Warehouses = await _context.Warehouse.ToListAsync();
            ViewBag.Items = await _context.Item.ToListAsync();
            ViewBag.Customers = await _context.Customer.ToListAsync();
            ViewBag.Transporters = await _context.Transporter.ToListAsync();

            ViewBag.Sales = salesDetail;

            return View("~/Views/Sales/SalesVoucher.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesRate(int itemId)
        {
            try
            {
                var item = await _context.Item.FindAsync(itemId);
                if (item != null)
                {
                    return Json(new { salesRate = item.sale_rate });
                }
                return Json(new { salesRate = (decimal?)null });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, int page = 1, int pageSize = 5)
        {
            var sales = await _context.StockMaster
                .Include(u => u.User)
                .Include(v => v.Customer)
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (sales == null)
            {
                _notyf.Error("Sales voucher not found");
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

            int totalSales = await _context.StockMaster
                .CountAsync(d => d.etype == "Sales");

            var salesData = await _context.StockMaster
                .Where(j => j.etype == "Sales")
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
                    CustomerName = j.Customer != null ? j.Customer.name : null,
                })
                .ToListAsync();

            ViewBag.TotalItems = totalSales;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.ActiveTab = "form";

            ViewBag.Warehouses = await _context.Warehouse.ToListAsync();
            ViewBag.Items = await _context.Item.ToListAsync();
            ViewBag.Customers = await _context.Customer.ToListAsync(); // FIXED TYPO
            ViewBag.Transporters = await _context.Transporter.ToListAsync();

            ViewBag.Sales = salesData;

            return View("~/Views/Sales/SalesVoucher.cshtml", model);
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
                    _notyf.Error("Sales voucher not found");
                    return NotFound();
                }

                // Update customer balance
                if (sales.customerId.HasValue)
                {
                    var customer = await _context.Customer.FirstOrDefaultAsync(c => c.Id == sales.customerId);
                    if (customer != null)
                    {
                        customer.current_balance -= sales.net_amount;
                        _context.Update(customer);
                    }
                }
                var paymentVoucher = await _context.PaymentVoucher.FirstOrDefaultAsync(p => p.customerId == sales.customerId && p.amount == sales.net_amount);
                if (paymentVoucher != null)
                    _context.PaymentVoucher.Remove(paymentVoucher);
                // Delete journal entries
                var journalEntry = await _context.JournalEntry
                    .FirstOrDefaultAsync(je => je.etype == "Sales" && je.description == $"Sales Entry for StockMaster {id}");

                if (journalEntry != null)
                {
                    var journalDetails = _context.JournalDetail.Where(jd => jd.journalEntryId == journalEntry.Id);
                    var ledgerEntries = _context.Ledger.Where(l => l.journalEntryId == journalEntry.Id);

                    _context.JournalDetail.RemoveRange(journalDetails);
                    _context.Ledger.RemoveRange(ledgerEntries);
                    _context.JournalEntry.Remove(journalEntry);
                }

                // Delete stock details and master
                var details = _context.StockDetail.Where(d => d.StockMasterId == id);
                _context.StockDetail.RemoveRange(details);
                _context.StockMaster.Remove(sales);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _notyf.Success("Sales Voucher Deleted Successfully");
                return RedirectToAction("SalesVoucher", new { activeTab = "list" });
            }
            catch (Exception ex)
            {
                _notyf.Error($"An Error Occurred: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> Create(PurchaseViewModel pvm)
        {
            try
            {
                // Get session data
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

                // Validate customer selection
                if (!pvm.StockMaster.customerId.HasValue || pvm.StockMaster.customerId.Value == 0)
                {
                    _notyf.Error("Please select a customer");
                    return RedirectToAction("SalesVoucher", new { activeTab = "form" });
                }

                // Validate stock details
                if (pvm.StockDetail == null || !pvm.StockDetail.Any())
                {
                    _notyf.Error("Please add at least one item to the sales voucher");
                    return RedirectToAction("SalesVoucher", new { activeTab = "form" });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Get Chart of Accounts
                    var cashInHand = await _context.ChartOfAccount
                        .FirstOrDefaultAsync(c => c.name == "Cash in hand" && c.companyId == companyId);
                    var salesRevenue = await _context.ChartOfAccount
                        .FirstOrDefaultAsync(c => c.name == "Sales Revenue" && c.companyId == companyId);

                    if (cashInHand == null || salesRevenue == null)
                    {
                        _notyf.Error("Required chart of accounts (Cash in hand or Sales Revenue) not found.");
                        await transaction.RollbackAsync();
                        return BadRequest("Required chart of accounts not found for the company.");
                    }

                    int cashInHandId = cashInHand.Id;
                    int salesRevenueId = salesRevenue.Id;

                    // ========== UPDATE EXISTING SALES ==========
                    if (pvm.StockMaster.Id > 0)
                    {
                        var existingSales = await _context.StockMaster
                            .FirstOrDefaultAsync(x => x.Id == pvm.StockMaster.Id);

                        if (existingSales == null)
                        {
                            _notyf.Error("Sales voucher not found");
                            await transaction.RollbackAsync();
                            return NotFound();
                        }

                        // Store old values
                        decimal oldNetAmount = existingSales.net_amount;
                        int? oldCustomerId = existingSales.customerId;

                        // Adjust old customer balance
                        if (oldCustomerId.HasValue)
                        {
                            var oldCustomer = await _context.Customer.FirstOrDefaultAsync(v => v.Id == oldCustomerId);
                            if (oldCustomer != null)
                            {
                                oldCustomer.current_balance -= oldNetAmount;
                                _context.Update(oldCustomer);
                            }
                        }

                        // Adjust new customer balance
                        var newCustomer = await _context.Customer.FirstOrDefaultAsync(v => v.Id == pvm.StockMaster.customerId);
                        if (newCustomer != null)
                        {
                            newCustomer.current_balance += pvm.StockMaster.net_amount;
                            _context.Update(newCustomer);
                        }

                        // Update StockMaster
                        existingSales.current_date = pvm.StockMaster.current_date;
                        existingSales.posted_date = pvm.StockMaster.posted_date;
                        existingSales.due_date = pvm.StockMaster.due_date;
                        existingSales.userId = userId;
                        existingSales.companyId = companyId;
                        existingSales.customerId = pvm.StockMaster.customerId;
                        existingSales.etype = "Sales";
                        existingSales.total_amount = pvm.StockMaster.total_amount;
                        existingSales.discount_amount = pvm.StockMaster.discount_amount;
                        existingSales.tax_amount = pvm.StockMaster.tax_amount;
                        existingSales.net_amount = pvm.StockMaster.net_amount;
                        existingSales.remarks = pvm.StockMaster.remarks;

                        _context.Update(existingSales);
                        await _context.SaveChangesAsync();
                        var paymentVoucher = await _context.PaymentVoucher.FirstOrDefaultAsync(p => p.customerId == oldCustomerId && p.amount == oldNetAmount);
                        if (paymentVoucher != null)
                        {
                            paymentVoucher.customerId = pvm.StockMaster.customerId;
                            paymentVoucher.amount = pvm.StockMaster.net_amount;
                            paymentVoucher.companyId = companyId;
                            paymentVoucher.status = true;
                            _context.Update(paymentVoucher);
                        }


                        // Remove old StockDetail, Journal, and Ledger
                        var existingDetails = _context.StockDetail.Where(d => d.StockMasterId == existingSales.Id);
                        _context.StockDetail.RemoveRange(existingDetails);

                        var existingJournalEntry = await _context.JournalEntry
                            .FirstOrDefaultAsync(je => je.etype == "Sales" &&
                                je.description == $"Sales Entry for StockMaster {existingSales.Id}");

                        if (existingJournalEntry != null)
                        {
                            var existingJournalDetails = _context.JournalDetail
                                .Where(jd => jd.journalEntryId == existingJournalEntry.Id);
                            var existingLedgers = _context.Ledger
                                .Where(l => l.journalEntryId == existingJournalEntry.Id);

                            _context.JournalDetail.RemoveRange(existingJournalDetails);
                            _context.Ledger.RemoveRange(existingLedgers);
                            _context.JournalEntry.Remove(existingJournalEntry);
                        }

                        await _context.SaveChangesAsync();

                        // Create new JournalEntry
                        var journalEntry = new JournalEntry
                        {
                            current_date = pvm.StockMaster.current_date,
                            due_date = pvm.StockMaster.due_date,
                            posted_date = pvm.StockMaster.posted_date,
                            companyId = companyId,
                            userId = userId,
                            etype = "Sales",
                            description = $"Sales Entry for StockMaster {existingSales.Id}",
                            total_debit = pvm.StockMaster.net_amount,
                            total_credit = pvm.StockMaster.net_amount
                        };
                        _context.JournalEntry.Add(journalEntry);
                        await _context.SaveChangesAsync();

                        // JournalDetail entries
                        var journalDetails = new List<JournalDetail>
                {
                    new JournalDetail
                    {
                        current_date = pvm.StockMaster.current_date,
                        journalEntryId = journalEntry.Id,
                        chartOfAccountId = cashInHandId,
                        debit_amount = pvm.StockMaster.net_amount,
                        credit_amount = 0.00m,
                        description = "Sales Receivable"
                    },
                    new JournalDetail
                    {
                        current_date = pvm.StockMaster.current_date,
                        journalEntryId = journalEntry.Id,
                        chartOfAccountId = salesRevenueId,
                        debit_amount = 0.00m,
                        credit_amount = pvm.StockMaster.net_amount,
                        description = "Sales Revenue"
                    }
                };
                        _context.JournalDetail.AddRange(journalDetails);

                        // Ledger entries
                        decimal cashRunningBalance = await _context.Ledger
                            .Where(l => l.chartOfAccountId == cashInHandId && l.companyId == companyId)
                            .OrderByDescending(l => l.Id)
                            .Select(l => l.running_balance)
                            .FirstOrDefaultAsync();

                        decimal revenueRunningBalance = await _context.Ledger
                            .Where(l => l.chartOfAccountId == salesRevenueId && l.companyId == companyId)
                            .OrderByDescending(l => l.Id)
                            .Select(l => l.running_balance)
                            .FirstOrDefaultAsync();

                        var ledgerEntries = new List<Ledger>
                {
                    new Ledger
                    {
                        current_date = pvm.StockMaster.current_date,
                        companyId = companyId,
                        chartOfAccountId = cashInHandId,
                        journalEntryId = journalEntry.Id,
                        debit_amount = pvm.StockMaster.net_amount,
                        credit_amount = 0.00m,
                        running_balance = cashRunningBalance + pvm.StockMaster.net_amount,
                        description = "Sales Receivable"
                    },
                    new Ledger
                    {
                        current_date = pvm.StockMaster.current_date,
                        companyId = companyId,
                        chartOfAccountId = salesRevenueId,
                        journalEntryId = journalEntry.Id,
                        debit_amount = 0.00m,
                        credit_amount = pvm.StockMaster.net_amount,
                        running_balance = revenueRunningBalance + pvm.StockMaster.net_amount,
                        description = "Sales Revenue"
                    }
                };
                        _context.Ledger.AddRange(ledgerEntries);

                        // Add new StockDetail
                        foreach (var d in pvm.StockDetail)
                        {
                            d.StockMasterId = existingSales.Id;
                            _context.StockDetail.Add(d);
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        _notyf.Success("Sales Voucher Updated Successfully");
                    }
                    else
                    {

                        pvm.StockMaster.venderId = null;
                        pvm.StockMaster.transporterId = null;
                        _context.StockMaster.Add(pvm.StockMaster);
                        await _context.SaveChangesAsync();

                        var customer = await _context.Customer.FirstOrDefaultAsync(v => v.Id == pvm.StockMaster.customerId);
                        if (customer != null)
                        {
                            customer.current_balance += pvm.StockMaster.net_amount;
                            _context.Update(customer);
                        }
                        pvm.PaymentVoucher.venderId = null;
                        pvm.PaymentVoucher.bankAccountId = null;
                        pvm.PaymentVoucher.companyId = companyId;
                        pvm.PaymentVoucher.customerId = pvm.StockMaster.customerId;
                        pvm.PaymentVoucher.status = true;
                        pvm.PaymentVoucher.amount = pvm.StockMaster.net_amount;
                        _context.PaymentVoucher.Add(pvm.PaymentVoucher);
                        await _context.SaveChangesAsync();

                        // Create JournalEntry
                        var journalEntry = new JournalEntry
                        {
                            current_date = pvm.StockMaster.current_date,
                            due_date = pvm.StockMaster.due_date,
                            posted_date = pvm.StockMaster.posted_date,
                            companyId = companyId,
                            userId = userId,
                            etype = "Sales",
                            description = $"Sales Entry for StockMaster {pvm.StockMaster.Id}",
                            total_debit = pvm.StockMaster.net_amount,
                            total_credit = pvm.StockMaster.net_amount
                        };
                        _context.JournalEntry.Add(journalEntry);
                        await _context.SaveChangesAsync();

                        // JournalDetail entries
                        var journalDetails = new List<JournalDetail>
                {
                    new JournalDetail
                    {
                        current_date = pvm.StockMaster.current_date,
                        journalEntryId = journalEntry.Id,
                        chartOfAccountId = cashInHandId,
                        debit_amount = pvm.StockMaster.net_amount,
                        credit_amount = 0.00m,
                        description = "Sales Receivable"
                    },
                    new JournalDetail
                    {
                        current_date = pvm.StockMaster.current_date,
                        journalEntryId = journalEntry.Id,
                        chartOfAccountId = salesRevenueId,
                        debit_amount = 0.00m,
                        credit_amount = pvm.StockMaster.net_amount,
                        description = "Sales Revenue"
                    }
                };
                        _context.JournalDetail.AddRange(journalDetails);

                        // Ledger entries
                        decimal cashRunningBalance = await _context.Ledger
                            .Where(l => l.chartOfAccountId == cashInHandId && l.companyId == companyId)
                            .OrderByDescending(l => l.Id)
                            .Select(l => l.running_balance)
                            .FirstOrDefaultAsync();

                        decimal revenueRunningBalance = await _context.Ledger
                            .Where(l => l.chartOfAccountId == salesRevenueId && l.companyId == companyId)
                            .OrderByDescending(l => l.Id)
                            .Select(l => l.running_balance)
                            .FirstOrDefaultAsync();

                        var ledgerEntries = new List<Ledger>
                {
                    new Ledger
                    {
                        current_date = pvm.StockMaster.current_date,
                        companyId = companyId,
                        chartOfAccountId = cashInHandId,
                        journalEntryId = journalEntry.Id,
                        debit_amount = pvm.StockMaster.net_amount,
                        credit_amount = 0.00m,
                        running_balance = cashRunningBalance + pvm.StockMaster.net_amount,
                        description = "Sales Receivable"
                    },
                    new Ledger
                    {
                        current_date = pvm.StockMaster.current_date,
                        companyId = companyId,
                        chartOfAccountId = salesRevenueId,
                        journalEntryId = journalEntry.Id,
                        debit_amount = 0.00m,
                        credit_amount = pvm.StockMaster.net_amount,
                        running_balance = revenueRunningBalance + pvm.StockMaster.net_amount,
                        description = "Sales Revenue"
                    }
                };
                        _context.Ledger.AddRange(ledgerEntries);

                        // Add StockDetail
                        foreach (var d in pvm.StockDetail)
                        {
                            d.StockMasterId = pvm.StockMaster.Id;
                            _context.StockDetail.Add(d);
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        _notyf.Success("Sales Voucher Created Successfully");
                    }

                    return RedirectToAction("SalesVoucher", new { activeTab = "list" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Error saving Sales voucher: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                _notyf.Error($"An Error Occurred: {ex.Message}");
                var inner = ex.InnerException != null ? ex.InnerException.Message : "";
                return BadRequest($"{ex.Message} - {inner}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PrintSalesVoucher(int id)
        {
            var sales = await _context.StockMaster
                .Include(s => s.Customer)
                .Include(s => s.StockDetail)
                    .ThenInclude(d => d.Item)
                .Include(s => s.StockDetail)
                    .ThenInclude(d => d.Warehouse)
                .FirstOrDefaultAsync(s => s.Id == id && s.etype == "Sales");

            if (sales == null)
            {
                _notyf.Error("Sales voucher not found.");
                return NotFound();
            }

            var model = new PurchaseViewModel
            {
                StockMaster = sales,
                StockDetail = sales.StockDetail.ToList()
            };

            // Supply lookup data for your view
            ViewData["Warehouses"] = await _context.Warehouse.ToListAsync();
            ViewData["Items"] = await _context.Item.ToListAsync();
            ViewData["Customers"] = await _context.Customer.ToListAsync();

            return new ViewAsPdf("_PrintSalesVoucher", model)
            {
                FileName = $"Sales_Voucher_{id}.pdf",
                PageSize = Size.A4,
                PageOrientation = Orientation.Portrait,
                PageMargins = { Left = 15, Right = 15, Top = 20, Bottom = 20 }
            };
        }

    }
}