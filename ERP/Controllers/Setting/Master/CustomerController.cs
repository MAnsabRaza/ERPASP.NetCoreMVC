using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers.Setting.Master
{
    public class CustomerController : Controller
    {
        private readonly AppDbContext _context;
        public CustomerController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Customer(string searchString, int page = 1, int pageSize = 5)
        {
            var query = _context.Customer.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.name.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var customerList = await query.
                Include(c => c.Company).
                OrderBy(c => c.Id).
                Skip((page - 1) * pageSize).Take(pageSize).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;

            var model = new Customer
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            ViewBag.companyList = await _context.Company.ToListAsync();
            ViewBag.Customer = customerList;
            //return View("Customer",model);

            return View("~/Views/Setting/Master/Customer.cshtml", model);
        }
        [HttpPost]
        public async Task<IActionResult> Create(Customer customer)
        {
            try
            {
                if (customer.Id > 0)
                {
                    var existingCustomer = await _context.Customer.FindAsync(customer.Id);
                    if (existingCustomer != null)
                    {
                        existingCustomer.current_date = customer.current_date;
                        existingCustomer.name = customer.name;
                        existingCustomer.email = customer.email;
                        existingCustomer.address = customer.address;
                        existingCustomer.city = customer.city;
                        existingCustomer.status = customer.status;
                        existingCustomer.country = customer.country;
                        existingCustomer.phone = customer.phone;
                        existingCustomer.credit_limit = customer.credit_limit;
                        existingCustomer.current_balance = customer.current_balance;
                        existingCustomer.companyId = customer.companyId;
                        _context.Update(existingCustomer);
                        await _context.SaveChangesAsync();
                    }

                }
                else
                {
                    _context.Customer.Add(customer);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("Customer");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, string searchString, int page = 1, int pageSize = 5)
        {
            var customer = await _context.Customer.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            var query = _context.Customer.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.name.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var customerList = await query.
                Include(c => c.Company).
                OrderBy(c => c.Id).
                Skip((page - 1) * pageSize).Take(pageSize).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            ViewBag.companyList = await _context.Company.ToListAsync();
            ViewBag.Customer = customerList;
            //return View("Customer", customer);
            return View("~/Views/Setting/Master/Customer.cshtml", customer);
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _context.Customer.FindAsync(id);
            if (customer != null)
            {
                _context.Customer.Remove(customer);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Customer");
        }
    }
}
