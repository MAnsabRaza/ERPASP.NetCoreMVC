using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

namespace ERP.Controllers.Setting.Account
{
    public class PaymentVoucherController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;
        public PaymentVoucherController(AppDbContext context,INotyfService notyf) {
            _notyf = notyf;
            _context = context;
        }
        public async Task<IActionResult> PaymentVoucher(string searchString,int page=1,int pageSize=5)
        {
            var query=_context.PaymentVoucher.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(pv => pv.method.Contains(searchString));
            }

            var totalItems = await query.CountAsync();
            var paymentVoucherList=await query.
                Include(c=>c.Company).
                Include(v=>v.Vender).
                Include(b=>b.Bank).
                OrderBy(pv=>pv.Id).Skip((page-1)*pageSize).Take(pageSize).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            var model = new PaymentVoucher
            {
                current_date = DateOnly.FromDateTime(DateTime.Now),
                voucher_date = DateOnly.FromDateTime(DateTime.Now),
            };
            ViewBag.PaymentVoucher=paymentVoucherList;
            ViewBag.companyList = await _context.Company.ToListAsync();
            ViewBag.venderList = await _context.Vender.ToListAsync();
            ViewBag.bankList=await _context.Bank.ToListAsync();
            return View("~/Views/Setting/Account/PaymentVoucher.cshtml", model);
            //return View("PaymentVoucher",model);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string searchString, int page = 1,int pageSize = 5)
        {
            var paymentVoucher = await _context.PaymentVoucher.FindAsync(id);
            if(paymentVoucher == null)
            {
                return NotFound();
            }
            var query = _context.PaymentVoucher.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(pv => pv.method.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var paymentVoucherList = await query.
                Include(c => c.Company).
                Include(v => v.Vender).
                Include(b => b.Bank).
                OrderBy(pv => pv.Id).Skip((page - 1) * pageSize).Take(pageSize).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            ViewBag.PaymentVoucher = paymentVoucherList;
            ViewBag.companyList = await _context.Company.
                Where(c=>c.status==true).
                ToListAsync();
            ViewBag.venderList = await _context.Vender.
                Where(v => v.status == true).
                ToListAsync();
            ViewBag.bankList = await _context.Bank.
                Where(b => b.status == true).
                ToListAsync();
            //return View("PaymentVoucher",paymentVoucher);
            return View("~/Views/Setting/Account/PaymentVoucher.cshtml", paymentVoucher);
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var paymentVoucher = await _context.PaymentVoucher.FindAsync(id);
            if(paymentVoucher != null)
            {
                _context.PaymentVoucher.Remove(paymentVoucher);
                await _context.SaveChangesAsync();
                _notyf.Success("Payment Voucher Delete Successfully");
            }
            return RedirectToAction("PaymentVoucher");
        }
        [HttpPost]
        public async Task<IActionResult> Create(PaymentVoucher paymentVoucher)
        {
            try
            {
                if(paymentVoucher.Id > 0)
                {
                    var existingPayment = await _context.PaymentVoucher.FindAsync(paymentVoucher.Id);
                    if(existingPayment != null)
                    {
                        existingPayment.current_date = paymentVoucher.current_date;
                        existingPayment.voucher_date = paymentVoucher.voucher_date;
                        existingPayment.method = paymentVoucher.method;
                        existingPayment.amount = paymentVoucher.amount;
                        existingPayment.status = paymentVoucher.status;
                        existingPayment.companyId=paymentVoucher.companyId;
                        existingPayment.venderId=paymentVoucher.venderId;
                        existingPayment.bankAccountId = paymentVoucher.bankAccountId;
                        _context.Update(existingPayment);
                        await _context.SaveChangesAsync();
                        _notyf.Success("Payment Voucher Update Successfully");
                    }
                }
                else
                {
                    _context.PaymentVoucher.Add(paymentVoucher);
                    await _context.SaveChangesAsync();
                    _notyf.Success("Payment Voucher Create Successfully");
                }
                return RedirectToAction("PaymentVoucher");
            }
            catch(Exception ex)
            {
                _notyf.Error($"An Error Occurred: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }
    }
}
