using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ERP.Controllers.Setting
{
    public class CompanyController : Controller
    {
        private readonly AppDbContext _context;
        public CompanyController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Company()
        {
            var model = new Company
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            ViewBag.Company = await _context.Company.ToListAsync();
            return View("~/Views/Setting/UserManagement/Company.cshtml", model);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var company = await _context.Company.FindAsync(id);
            if (company == null)
            {
                return NotFound();
            }
            ViewBag.Company = _context.Company.ToListAsync();
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
                    }
                }
                else
                {
                    _context.Company.Add(company);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("Company");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
