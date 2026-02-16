using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace ERP.Controllers.Sales
{
    public class SalesOrderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;

        public SalesOrderController(AppDbContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }
        public async Task<IActionResult> SalesOrder(
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

            int totalOrders = await _context.StockMaster
                .CountAsync(d => d.etype == "SalesOrder");

            var orderList = await _context.StockMaster
                .Where(j => j.etype == "SalesOrder")
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

            ViewBag.TotalItems = totalOrders;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.ActiveTab = activeTab;
            ViewBag.Warehouses = await _context.Warehouse.ToListAsync();
            ViewBag.Items = await _context.Item.ToListAsync();
            ViewBag.Customers = await _context.Customer.ToListAsync();
            ViewBag.Sales = orderList;

            return View("~/Views/Sales/SalesOrderVoucher.cshtml", model);
        }

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

                // ✅ Basic Validation
                if (!pvm.StockMaster.customerId.HasValue || pvm.StockMaster.customerId == 0)
                {
                    _notyf.Error("Please select a customer.");
                    return RedirectToAction("SalesOrderVoucher");
                }

                if (pvm.StockDetail == null || !pvm.StockDetail.Any())
                {
                    _notyf.Error("Please add at least one item.");
                    return RedirectToAction("SalesOrderVoucher");
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    if (pvm.StockMaster.Id == 0)
                    {
                        // STEP 1: StockMaster insert
                        pvm.StockMaster.venderId = null; // ✅ NULL
                        pvm.StockMaster.transporterId = null; // ✅ NULL
                        // ✅ etype form se aayega "SalesOrder"
                        _context.StockMaster.Add(pvm.StockMaster);
                        await _context.SaveChangesAsync();

                        // STEP 2: Sirf StockDetail insert
                        foreach (var detail in pvm.StockDetail)
                        {
                            detail.StockMasterId = pvm.StockMaster.Id;
                            _context.StockDetail.Add(detail);
                        }


                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _notyf.Success("Sales Order Created Successfully");
                    }

                    else
                    {
                        var existingOrder = await _context.StockMaster
                            .FirstOrDefaultAsync(x => x.Id == pvm.StockMaster.Id);

                        if (existingOrder == null)
                        {
                            _notyf.Error("Sales Order not found.");
                            return NotFound();
                        }

                        // STEP 1: StockMaster update
                        existingOrder.current_date = pvm.StockMaster.current_date;
                        existingOrder.posted_date = pvm.StockMaster.posted_date;
                        existingOrder.due_date = pvm.StockMaster.due_date;
                        existingOrder.userId = userId;
                        existingOrder.companyId = companyId;
                        existingOrder.customerId = pvm.StockMaster.customerId;
                        existingOrder.venderId = null; // ✅ hamesha NULL
                        existingOrder.transporterId = null; // ✅ hamesha NULL
                        existingOrder.etype = "SalesOrder"; // ✅ fix
                        existingOrder.total_amount = pvm.StockMaster.total_amount;
                        existingOrder.discount_amount = pvm.StockMaster.discount_amount;
                        existingOrder.tax_amount = pvm.StockMaster.tax_amount;
                        existingOrder.net_amount = pvm.StockMaster.net_amount;
                        existingOrder.remarks = pvm.StockMaster.remarks;
                        _context.Update(existingOrder);

                        // STEP 2: Purani StockDetail delete
                        var oldDetails = _context.StockDetail
                            .Where(d => d.StockMasterId == existingOrder.Id);
                        _context.StockDetail.RemoveRange(oldDetails);

                        // STEP 3: Naye StockDetail add
                        foreach (var detail in pvm.StockDetail)
                        {
                            detail.StockMasterId = existingOrder.Id;
                            _context.StockDetail.Add(detail);
                        }


                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _notyf.Success("Sales Order Updated Successfully");
                    }

                    return RedirectToAction("SalesOrder", new { activeTab = "list" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Error saving sales order: {ex.Message}", ex);
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
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var order = await _context.StockMaster.FindAsync(id);
                if (order == null)
                {
                    _notyf.Error("Sales Order not found.");
                    return NotFound();
                }

                var details = _context.StockDetail
                    .Where(d => d.StockMasterId == id);
                _context.StockDetail.RemoveRange(details);

                // STEP 2: StockMaster delete
                _context.StockMaster.Remove(order);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _notyf.Success("Sales Order Deleted Successfully");

                return RedirectToAction("SalesOrder", new { activeTab = "list" });
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
            var order = await _context.StockMaster
                .Include(u => u.User)
                .Include(c => c.Customer)
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (order == null)
            {
                _notyf.Error("Sales Order not found.");
                return NotFound();
            }

            var orderDetail = await _context.StockDetail
                .Include(it => it.Item)
                .Include(w => w.Warehouse)
                .Where(d => d.StockMasterId == id)
                .ToListAsync();

            var model = new PurchaseViewModel
            {
                StockMaster = order,
                StockDetail = orderDetail
            };

            int totalOrders = await _context.StockMaster
                .CountAsync(d => d.etype == "SalesOrder");

            var orderList = await _context.StockMaster
                .Where(j => j.etype == "SalesOrder")
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

            ViewBag.TotalItems = totalOrders;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.ActiveTab = "form";
            ViewBag.Warehouses = await _context.Warehouse.ToListAsync();
            ViewBag.Items = await _context.Item.ToListAsync();
            ViewBag.Customers = await _context.Customer.ToListAsync();
            ViewBag.Sales = orderList;

            return View("~/Views/Sales/SalesOrderVoucher.cshtml", model);
        }
        [HttpGet]
        public async Task<IActionResult> ItemModal()
        {
            var model = new Item
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };

            ViewBag.categoryList = await _context.Category
                .Where(c => c.status == true).ToListAsync();
            ViewBag.brandList = await _context.Brand
                .Where(b => b.status == true).ToListAsync();
            ViewBag.uomList = await _context.UOM
                .Where(u => u.status == true).ToListAsync();
            ViewBag.subCategoryList = await _context.SubCategory
                .Where(sb => sb.status == true).ToListAsync();

            return PartialView("~/Views/Shared/_ItemModal.cshtml", model);
        }
    }
}