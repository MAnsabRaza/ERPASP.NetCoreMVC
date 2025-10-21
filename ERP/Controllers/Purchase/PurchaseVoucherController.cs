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
            return View("~/Views/Purchase/PurchaseVoucher.cshtml",model);
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
                pvm.StockMaster.companyId= companyId;
                pvm.StockMaster.userId = userId;
                if (pvm.StockMaster.Id > 0)
                {
                    var existingPurchase = await _context.StockMaster.FirstOrDefaultAsync(x => x.Id == pvm.StockMaster.Id);
                    if(existingPurchase != null)
                    {
                        existingPurchase.current_date = pvm.StockMaster.current_date;
                        existingPurchase.posted_date = pvm.StockMaster.posted_date;
                        existingPurchase.due_date = pvm.StockMaster.due_date;
                        existingPurchase.userId = userId;
                        existingPurchase.companyId=companyId;
                        existingPurchase.venderId = pvm.StockMaster.venderId;
                        existingPurchase.transporterId = pvm.StockMaster.transporterId;
                        existingPurchase.etype=pvm.StockMaster.etype;
                        existingPurchase.etype = pvm.StockMaster.etype;
                        existingPurchase.total_amount = pvm.StockMaster.total_amount;
                        existingPurchase.discount_amount = pvm.StockMaster.discount_amount;
                        existingPurchase.net_amount = pvm.StockMaster.net_amount;
                        existingPurchase.remarks = pvm.StockMaster.remarks;
                        _context.Update(existingPurchase);
                        await _context.SaveChangesAsync();

                        var existingDetails = _context.StockDetail.Where(d => d.stockMasterId == existingPurchase.Id);
                        _context.StockDetail.RemoveRange(existingDetails);
                        await _context.SaveChangesAsync();

                        foreach (var d in pvm.StockDetail)
                        {
                            d.stockMasterId = existingPurchase.Id;
                            _context.StockDetail.Add(d);
                        }
                        await _context.SaveChangesAsync();
                        _notyf.Success("Purchase Voucher Update Successfully");
                    }
                }
                else
                {
                    pvm.StockMaster.customerId = null;
                    _context.StockMaster.Add(pvm.StockMaster);
                    await _context.SaveChangesAsync();

                    foreach (var d in pvm.StockDetail)
                    {
                        d.stockMasterId = pvm.StockMaster.Id;
                        _context.StockDetail.Add(d);
                    }
                    await _context.SaveChangesAsync();
                    _notyf.Success("Purchase Voucher Create Successfully");
                }
                return RedirectToAction("PurchaseVoucher");
            }
            catch(Exception ex)
            {
                _notyf.Error($"An Error Occurred: {ex.Message}");
                var inner = ex.InnerException != null ? ex.InnerException.Message : "";
                return BadRequest($"{ex.Message} - {inner}");
            }
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var purchase = await _context.StockMaster.FindAsync(id);
            if (purchase != null)
            {
                var detail = _context.StockDetail.Where(d => d.stockMasterId== id);
                _context.StockDetail.RemoveRange(detail);
                _context.StockMaster.Remove(purchase);
                await _context.SaveChangesAsync();
                _notyf.Success("Purchase Voucher Delete Successfully");
            }
            return RedirectToAction("Journal");
        }
    }
}

