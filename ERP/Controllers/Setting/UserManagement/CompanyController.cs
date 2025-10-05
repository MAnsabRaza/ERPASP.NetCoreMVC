using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Drawing.Printing;

namespace ERP.Controllers.Setting.UserManagement
{
    public class CompanyController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;
        public CompanyController(AppDbContext context,INotyfService notyf)
        {
            _notyf = notyf;
            _context = context;
        }
        public async Task<IActionResult> Company(string searchString, int page = 1, int pageSize = 5)
        {
            var query = _context.Company.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.company_name.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var companyList = await query.
                OrderBy(c => c.Id).Skip((page - 1) * pageSize).Take(pageSize).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;

            var model = new Company
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            ViewBag.Company = companyList;
            return View("~/Views/Setting/UserManagement/Company.cshtml", model);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string searchString, int page = 1, int pageSize = 5)
        {
            var company = await _context.Company.FindAsync(id);
            if (company == null)
            {
                return NotFound();
            }
            var query = _context.Company.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.company_name.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var companyList = await query.
            OrderBy(c => c.Id).Skip((page - 1) * pageSize).Take(pageSize).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            ViewBag.Company = companyList;
            return View("~/Views/Setting/UserManagement/Company.cshtml", company);
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var company = await _context.Company.FindAsync(id);
            if (company != null)
            {
                _context.Company.Remove(company);
                await _context.SaveChangesAsync();
                _notyf.Success("Company Delete Successfully");
            }
            return RedirectToAction("Company");
        }
        [HttpPost]
        public async Task<IActionResult> Create(Company company, IFormFile logoFile)
        {
            try
            {
                if (logoFile != null && logoFile.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        await logoFile.CopyToAsync(ms);
                        byte[] fileBytes = ms.ToArray();
                        company.logo = Convert.ToBase64String(fileBytes);
                    }
                }
                if (company.Id > 0)
                {
                    var existingCompany = await _context.Company.FindAsync(company.Id);
                    if (existingCompany != null)
                    {
                        existingCompany.current_date = company.current_date;
                        existingCompany.company_name = company.company_name;
                        existingCompany.address = company.address;
                        existingCompany.status = company.status;
                        existingCompany.website_path = company.website_path;
                        existingCompany.company_email = company.company_email;
                        existingCompany.city = company.city;
                        existingCompany.country = company.country;
                        existingCompany.zipcode = company.zipcode;
                        existingCompany.phone = company.phone;
                        if (!string.IsNullOrEmpty(company.logo))
                        {
                            existingCompany.logo = company.logo;
                        }
                        _context.Update(existingCompany);
                        await _context.SaveChangesAsync();
                        _notyf.Success("Company Update Successfully");
                    }
                }
                else
                {
                    _context.Company.Add(company);
                    await _context.SaveChangesAsync();
                    _notyf.Success("Company Create Successfully");
                }
                return RedirectToAction("Company");
            }
            catch (Exception ex)
            {
                _notyf.Error($"An Error Occurred: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }
    }
}
