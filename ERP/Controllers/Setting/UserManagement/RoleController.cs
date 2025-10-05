using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Drawing.Printing;

namespace ERP.Controllers.Setting.UserManagement
{
    public class RoleController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;
        public RoleController(AppDbContext context,INotyfService notyf)
        {
            _notyf = notyf;
            _context = context;
        }

        public async Task<IActionResult> Role(string searchString, int page = 1, int pageSize = 5)
        {
            var query = _context.Role.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(r => r.role_name.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var roleList = await query.
                OrderBy(r => r.Id).Skip((page - 1) * pageSize).Take(pageSize).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            var model = new Role
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            ViewBag.Role = roleList;
            return View("~/Views/Setting/UserManagement/Role.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, string searchString, int page = 1, int pageSize = 5)
        {
            var role = await _context.Role.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }
            var query = _context.Role.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(r => r.role_name.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var roleList = await query.
            OrderBy(r => r.Id).Skip((page - 1) * pageSize).Take(pageSize).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            ViewBag.Role = roleList;
            return View("~/Views/Setting/UserManagement/Role.cshtml", role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Role role)
        {
            try
            {
                if (role.Id > 0)
                {
                    var existingRole = await _context.Role.FindAsync(role.Id);
                    if (existingRole != null)
                    {
                        existingRole.role_name = role.role_name;
                        existingRole.status = role.status;
                        existingRole.current_date = role.current_date;
                        _context.Update(existingRole);
                        await _context.SaveChangesAsync();
                        _notyf.Success("Role Update Successfully");
                    }
                }
                else
                {
                    _context.Role.Add(role);
                    await _context.SaveChangesAsync();
                    _notyf.Success("Role Create Successfully");
                }
                return RedirectToAction("Role");
            }
            catch (Exception ex)
            {
                _notyf.Error($"An Error Occurred: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var roles = await _context.Role.FindAsync(id);
            if (roles != null)
            {
                _context.Role.Remove(roles);
                await _context.SaveChangesAsync();
                _notyf.Success("Role Delete Successfully");
            }
            return RedirectToAction("Role");
        }
    }
}
