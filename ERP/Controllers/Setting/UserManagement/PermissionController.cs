using ERP.Models.UserManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.ComponentModel;
using System.Drawing.Printing;

namespace ERP.Controllers.Setting.UserManagement
{
    public class PermissionController : Controller
    {
        private readonly AppDbContext _context;
        public PermissionController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Permission(int page = 1, int pageSize = 5)
        {
            var query = _context.Permission.AsQueryable();
            var totalItems = await query.CountAsync();
            var permissionList = await query.
                   Include(m => m.Module).
                Include(c => c.Component).
                Include(r => r.Role).
                OrderBy(p => p.Id).
                Skip((page - 1) * pageSize).
                Take(pageSize).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            var model = new Permission
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            ViewBag.moduleList = await _context.Module.Where(m => m.status == true).ToListAsync();
            ViewBag.componentList = await _context.Component.Where(c => c.status == true).ToListAsync();
            ViewBag.roleList = await _context.Role.Where(r => r.status == true).ToListAsync();
            ViewBag.Permission = permissionList;
            return View("~/Views/Setting/UserManagement/Permission.cshtml", model);
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var permission = await _context.Permission.FindAsync(id);
            if (permission != null)
            {
                _context.Permission.Remove(permission);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Permission");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id, int page = 1, int pageSize = 5)
        {

            var permission = await _context.Permission.FindAsync(id);
            if (permission == null)
            {
                return NotFound();
            }
            var query = _context.Permission.AsQueryable();
            var totalItems = await query.CountAsync();
            var permissionList = await query.
                   Include(m => m.Module).
                Include(c => c.Component).
                Include(r => r.Role).
            OrderBy(p => p.Id).
                Skip((page - 1) * pageSize).
                Take(pageSize).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.moduleList = await _context.Module.Where(m => m.status == true).ToListAsync();
            ViewBag.componentList = await _context.Component.Where(c => c.status == true).ToListAsync();
            ViewBag.roleList = await _context.Role.Where(r => r.status == true).ToListAsync();
            ViewBag.Permission = permissionList;
            return View("~/Views/Setting/UserManagement/Permission.cshtml", permission);
        }
        [HttpPost]
        public async Task<IActionResult> Create(Permission permission)
        {
            try
            {
                if (permission.Id > 0)
                {
                    var existingPermission = await _context.Permission.FindAsync(permission.Id);
                    if (existingPermission != null)
                    {
                        existingPermission.current_date = permission.current_date;
                        existingPermission.roleId = permission.roleId;
                        existingPermission.moduleId = permission.moduleId;
                        existingPermission.componentId = permission.componentId;
                        existingPermission.view = permission.view;
                        existingPermission.create = permission.create;
                        existingPermission.delete = permission.delete;
                        existingPermission.edit = permission.edit;
                        _context.Update(existingPermission);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    _context.Permission.Add(permission);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("Permission");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
