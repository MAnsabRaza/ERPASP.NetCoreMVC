using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Printing;

namespace ERP.Controllers.Setting
{
    public class ComponentController : Controller
    {
        private readonly AppDbContext _context;
        public ComponentController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Component(string searchString, int page = 1,int pageSize=5)
        {
            var query=_context.Component.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.component_name.Contains(searchString));
            }
            var totalItems=await query.CountAsync();
            var componetList= await query.
                Include(m=>m.Module).
                OrderBy(c=>c.Id).
                Skip((page-1)* pageSize).
                Take(pageSize).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;

            var model = new Component
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            ViewBag.Component = componetList;
            ViewBag.Module = await _context.Module.Where(m=>m.status==true).ToListAsync();
            return View("~/Views/Setting/UserManagement/Component.cshtml", model);
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var component = await _context.Component.FindAsync(id);
            if (component != null)
            {
                _context.Component.Remove(component);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Component");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string searchString,int page=1,int pageSize=5)
        {
            var component = await _context.Component.FindAsync(id);
            if (component == null)
            {
                return NotFound();
            }

            var query = _context.Component.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.component_name.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var componetList = await query.
                Include(m => m.Module).
            OrderBy(c => c.Id).
                Skip((page - 1) * pageSize).
                Take(pageSize).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;

            ViewBag.Component = componetList;
            ViewBag.Module = await _context.Module.Where(m => m.status == true).ToListAsync();
            return View("~/Views/Setting/UserManagement/Component.cshtml", component);
        }
        [HttpPost]
        public async Task<IActionResult> Create(Component component)
        {
            try
            {
                if (component.Id > 0)
                {
                    var existingComponent = await _context.Component.FindAsync(component.Id);
                    if (existingComponent != null)
                    {
                        existingComponent.current_date = component.current_date;
                        existingComponent.component_name = component.component_name;
                        existingComponent.moduleId = component.moduleId;
                        existingComponent.status = component.status;
                        _context.Update(existingComponent);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    _context.Component.Add(component);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("Component");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
