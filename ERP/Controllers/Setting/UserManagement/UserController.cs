using BCrypt.Net;
using ERP.Models.UserManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.Drawing.Printing;
using System.Security;

namespace ERP.Controllers.Setting.UserManagement
{
    public class UserController : Controller
    {
        private readonly AppDbContext _context;
        public UserController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> User(string searchString, int page = 1, int pageSize = 5)
        {
            var query = _context.User.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(u => u.name.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var userList = await query.
                Include(r => r.Role).
                Include(c => c.Company).
                OrderBy(u => u.Id).
                Skip((page - 1) * pageSize).
                Take(pageSize).
                ToListAsync();

            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;

            var model = new User
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };

            ViewBag.User = userList;
            ViewBag.companyList = await _context.Company.
                Where(c => c.status == true).
                ToListAsync();
            ViewBag.roleList = await _context.Role.
                Where(c => c.status == true).ToListAsync();
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
        public async Task<IActionResult> Edit(int id, string searchString, int page = 1, int pageSize = 5)
        {
            var user = await _context.User.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            var query = _context.User.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(u => u.name.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var userList = await query.
                Include(r => r.Role).
                Include(c => c.Company).
            OrderBy(u => u.Id).
                Skip((page - 1) * pageSize).
                Take(pageSize).
                ToListAsync();

            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;

            ViewBag.User = userList;
            ViewBag.companyList = await _context.Company.Where(c => c.status == true).ToListAsync();
            ViewBag.roleList = await _context.Role.Where(c => c.status == true).ToListAsync();
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
