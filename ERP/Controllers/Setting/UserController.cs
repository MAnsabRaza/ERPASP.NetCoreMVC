using BCrypt.Net;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security;

namespace ERP.Controllers.Setting
{
    public class UserController : Controller
    {
        private readonly AppDbContext _context;
        public UserController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> User()
        {
            var model = new User
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };

            ViewBag.User = await _context.User.
                Include(r => r.Role).
                Include(c => c.Company).
                ToListAsync();
            ViewBag.companyList = await _context.Company.ToListAsync();
            ViewBag.roleList = await _context.Role.ToListAsync();
            return View("~/Views/Setting/UserManagement/User.cshtml", model);
        }
        [HttpPost]
        public async Task<IActionResult> Create(User user, IFormFile logoFile)
        {
            try
            {
                if (logoFile != null && logoFile.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        await logoFile.CopyToAsync(ms);
                        byte[] fileBytes = ms.ToArray();
                        user.image = Convert.ToBase64String(fileBytes);
                    }
                }
                if (user.Id > 0)
                {
                    var existingUser = await _context.User.FindAsync(user.Id);
                    if (existingUser != null)
                    {
                        existingUser.current_date = user.current_date;
                        existingUser.name = user.name;
                        existingUser.companyId = user.companyId;
                        existingUser.roleId = user.roleId;
                        existingUser.address = user.address;
                        existingUser.phone_number = user.phone_number;
                        existingUser.email = user.email;
                        if (!string.IsNullOrWhiteSpace(user.password))
                        {
                            existingUser.password = BCrypt.Net.BCrypt.HashPassword(user.password);
                        }
                        if (!string.IsNullOrEmpty(user.image))
                        {
                            existingUser.image = user.image;
                        }
                        _context.Update(existingUser);
                        await _context.SaveChangesAsync();

                    }
                }
                else
                {
                    user.password = BCrypt.Net.BCrypt.HashPassword(user.password);
                    _context.User.Add(user);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("User");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.User.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            ViewBag.companyList = await _context.Company.ToListAsync();
            ViewBag.User = await _context.User.ToListAsync();
            ViewBag.roleList = await _context.Role.ToListAsync();
            return View("~/Views/Setting/UserManagement/User.cshtml", user);
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.User.FindAsync(id);
            if (user != null)
            {
                _context.User.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("User", user);
        }
    }
}
