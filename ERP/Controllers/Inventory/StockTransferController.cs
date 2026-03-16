using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers.Inventory
{
    public class StockTransferController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;

        public StockTransferController(AppDbContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }

        // ════════════════════════════════════════════════════
        // INDEX
        // ════════════════════════════════════════════════════
        public async Task<IActionResult> StockTransfer(int page = 1, int pageSize = 5, string activeTab = "form")
        {
            var model = new PurchaseViewModel
            {
                StockMaster = new StockMaster
                {
                    current_date = DateOnly.FromDateTime(DateTime.Now),
                    due_date = DateOnly.FromDateTime(DateTime.Now),
                    posted_date = DateOnly.FromDateTime(DateTime.Now),
                    etype = "stock_transfer"
                },
                StockDetail = new List<StockDetail>()
            };

            int total = await _context.StockMaster
                .CountAsync(d => d.etype == "stock_transfer");

            var list = await _context.StockMaster
                .Where(j => j.etype == "stock_transfer")
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
                    NetAmount = j.net_amount
                })
                .ToListAsync();

            ViewBag.TotalItems = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.ActiveTab = activeTab;
            ViewBag.Warehouses = await _context.Warehouse.ToListAsync();
            ViewBag.Items = await _context.Item.ToListAsync();
            ViewBag.TransferList = list;

            return View("~/Views/Inventory/StockTransfer.cshtml", model);
        }

        // ════════════════════════════════════════════════════
        // GET ITEM RATE + STOCK QTY
        // ════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetItemInfo(int itemId)
        {
            var item = await _context.Item.FindAsync(itemId);
            if (item == null) return Json(new { success = false });
            return Json(new { success = true, purchaseRate = item.purchase_rate, stockQty = item.qty });
        }

        // ════════════════════════════════════════════════════
        // CREATE
        // ════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> Create(PurchaseViewModel pvm,
                                                 List<int> fromWarehouseIds,
                                                 List<int> toWarehouseIds)
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
                pvm.StockMaster.etype = "stock_transfer";
                pvm.StockMaster.venderId = null;
                pvm.StockMaster.customerId = null;

                // ── Validate FROM != TO ──
                for (int i = 0; i < fromWarehouseIds.Count; i++)
                {
                    if (fromWarehouseIds[i] == toWarehouseIds[i])
                    {
                        _notyf.Error("From Warehouse and To Warehouse cannot be the same.");
                        return RedirectToAction("StockTransfer");
                    }
                }

                // ── Validate stock availability ──
                for (int i = 0; i < pvm.StockDetail.Count; i++)
                {
                    var detail = pvm.StockDetail[i];
                    var item = await _context.Item.FirstOrDefaultAsync(x => x.Id == detail.itemId);

                    if (item == null)
                    {
                        _notyf.Error($"Item ID {detail.itemId} not found.");
                        return RedirectToAction("StockTransfer");
                    }
                    if (item.qty <= 0)
                    {
                        _notyf.Error($"'{item.item_name}' has zero quantity in stock.");
                        return RedirectToAction("StockTransfer");
                    }
                    if (detail.qty > item.qty)
                    {
                        _notyf.Error($"'{item.item_name}' available quantity is {item.qty}.");
                        return RedirectToAction("StockTransfer");
                    }
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // ════════════════════════
                    // NEW TRANSFER
                    // ════════════════════════
                    if (pvm.StockMaster.Id == 0)
                    {
                        // STEP 1: StockMaster
                        _context.StockMaster.Add(pvm.StockMaster);
                        await _context.SaveChangesAsync();

                        // STEP 2: StockDetail — 2 rows per item (FROM -qty, TO +qty)
                        for (int i = 0; i < pvm.StockDetail.Count; i++)
                        {
                            var detail = pvm.StockDetail[i];
                            int fromWhId = fromWarehouseIds[i];
                            int toWhId = toWarehouseIds[i];
                            int absQty = (int)Math.Abs(detail.qty);

                            // ROW 1: FROM warehouse — MINUS qty
                            _context.StockDetail.Add(new StockDetail
                            {
                                current_date = pvm.StockMaster.current_date,
                                StockMasterId = pvm.StockMaster.Id,
                                warehouseId = fromWhId,
                                itemId = detail.itemId,
                                qty = -absQty,            // NEGATIVE
                                rate = detail.rate,
                                amount = detail.amount,
                                discount_percentage = detail.discount_percentage,
                                discount_amount = detail.discount_amount,
                                net_amount = detail.net_amount
                            });

                            // ROW 2: TO warehouse — PLUS qty
                            _context.StockDetail.Add(new StockDetail
                            {
                                current_date = pvm.StockMaster.current_date,
                                StockMasterId = pvm.StockMaster.Id,
                                warehouseId = toWhId,
                                itemId = detail.itemId,
                                qty = absQty,             // POSITIVE
                                rate = detail.rate,
                                amount = detail.amount,
                                discount_percentage = detail.discount_percentage,
                                discount_amount = detail.discount_amount,
                                net_amount = detail.net_amount
                            });

                            // STEP 3: Item total qty SAME rahay ga — NO update
                            // Warehouse wise tracking stock_detail se hogi
                        }

                        // ✅ NO JournalEntry — Stock Transfer mein journal nahi banta
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _notyf.Success("Stock Transfer Saved Successfully");
                    }

                    // ════════════════════════
                    // UPDATE TRANSFER
                    // ════════════════════════
                    else
                    {
                        var existing = await _context.StockMaster
                            .FirstOrDefaultAsync(x => x.Id == pvm.StockMaster.Id);

                        if (existing == null)
                        {
                            _notyf.Error("Record not found.");
                            return NotFound();
                        }

                        // STEP 1: Delete old StockDetail rows
                        var oldDetails = await _context.StockDetail
                            .Where(d => d.StockMasterId == existing.Id)
                            .ToListAsync();
                        _context.StockDetail.RemoveRange(oldDetails);

                        // STEP 2: Update StockMaster
                        existing.current_date = pvm.StockMaster.current_date;
                        existing.due_date = pvm.StockMaster.due_date;
                        existing.posted_date = pvm.StockMaster.posted_date;
                        existing.userId = userId;
                        existing.companyId = companyId;
                        existing.total_amount = pvm.StockMaster.total_amount;
                        existing.discount_amount = pvm.StockMaster.discount_amount;
                        existing.tax_amount = pvm.StockMaster.tax_amount;
                        existing.net_amount = pvm.StockMaster.net_amount;
                        existing.remarks = pvm.StockMaster.remarks;
                        _context.Update(existing);

                        await _context.SaveChangesAsync();

                        // STEP 3: Insert new StockDetail rows
                        for (int i = 0; i < pvm.StockDetail.Count; i++)
                        {
                            var detail = pvm.StockDetail[i];
                            int fromWhId = fromWarehouseIds[i];
                            int toWhId = toWarehouseIds[i];
                            int absQty = (int)Math.Abs(detail.qty);

                            // FROM warehouse — MINUS
                            _context.StockDetail.Add(new StockDetail
                            {
                                current_date = pvm.StockMaster.current_date,
                                StockMasterId = existing.Id,
                                warehouseId = fromWhId,
                                itemId = detail.itemId,
                                qty = -absQty,
                                rate = detail.rate,
                                amount = detail.amount,
                                discount_percentage = detail.discount_percentage,
                                discount_amount = detail.discount_amount,
                                net_amount = detail.net_amount
                            });

                            // TO warehouse — PLUS
                            _context.StockDetail.Add(new StockDetail
                            {
                                current_date = pvm.StockMaster.current_date,
                                StockMasterId = existing.Id,
                                warehouseId = toWhId,
                                itemId = detail.itemId,
                                qty = absQty,
                                rate = detail.rate,
                                amount = detail.amount,
                                discount_percentage = detail.discount_percentage,
                                discount_amount = detail.discount_amount,
                                net_amount = detail.net_amount
                            });
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _notyf.Success("Stock Transfer Updated Successfully");
                    }

                    return RedirectToAction("StockTransfer", new { activeTab = "list" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Error: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                _notyf.Error($"An Error Occurred: {ex.Message}");
                return BadRequest($"{ex.Message} - {ex.InnerException?.Message}");
            }
        }

        // ════════════════════════════════════════════════════
        // DELETE
        // ════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int page = 1, int pageSize = 5)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var master = await _context.StockMaster.FirstOrDefaultAsync(s => s.Id == id);
                if (master == null)
                {
                    _notyf.Error("Record not found.");
                    return RedirectToAction("StockTransfer");
                }

                // Delete StockDetail (both FROM and TO rows)
                var details = _context.StockDetail.Where(d => d.StockMasterId == id);
                _context.StockDetail.RemoveRange(details);

                // Delete StockMaster
                _context.StockMaster.Remove(master);

                // ✅ No journal to delete — transfer mein journal nahi hota

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _notyf.Success("Stock Transfer Deleted Successfully");
                return RedirectToAction("StockTransfer", new { page, pageSize, activeTab = "list" });
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("StockTransfer");
            }
        }

        // ════════════════════════════════════════════════════
        // EDIT
        // ════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Edit(int id, int page = 1, int pageSize = 5)
        {
            var master = await _context.StockMaster
                .Include(u => u.User)
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (master == null) return NotFound();

            // Fetch all stock_detail rows for this transfer
            var allDetails = await _context.StockDetail
                .Include(it => it.Item)
                .Include(w => w.Warehouse)
                .Where(d => d.StockMasterId == id)
                .ToListAsync();

            // Only FROM rows (negative qty) — one per item
            var fromDetails = allDetails.Where(d => d.qty < 0).ToList();

            var model = new PurchaseViewModel
            {
                StockMaster = master,
                StockDetail = fromDetails
            };

            int total = await _context.StockMaster.CountAsync(d => d.etype == "stock_transfer");

            var list = await _context.StockMaster
                .Where(j => j.etype == "stock_transfer")
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
                    NetAmount = j.net_amount
                })
                .ToListAsync();

            ViewBag.TotalItems = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.ActiveTab = "form";
            ViewBag.Warehouses = await _context.Warehouse.ToListAsync();
            ViewBag.Items = await _context.Item.ToListAsync();
            ViewBag.TransferList = list;

            // Pass FROM/TO warehouse mapping for edit mode
            // Key = itemId, Value = (fromWarehouseId, toWarehouseId)
            var toDetails = allDetails.Where(d => d.qty > 0).ToList();
            var warehouseMapping = new List<object>();
            foreach (var fromRow in fromDetails)
            {
                var toRow = toDetails.FirstOrDefault(t =>
                    t.itemId == fromRow.itemId && t.StockMasterId == fromRow.StockMasterId);
                warehouseMapping.Add(new
                {
                    itemId = fromRow.itemId,
                    fromWarehouseId = fromRow.warehouseId,
                    toWarehouseId = toRow?.warehouseId
                });
            }
            ViewBag.WarehouseMapping = System.Text.Json.JsonSerializer.Serialize(warehouseMapping);

            return View("~/Views/Inventory/StockTransfer.cshtml", model);
        }

        // ════════════════════════════════════════════════════
        // ITEM MODAL
        // ════════════════════════════════════════════════════
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
    }
}