using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Controllers
{
    public class ComponentController : Controller
    {
        private readonly AppDbContext _context;
        public ComponentController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Component()
        {
            var model = new Component
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            ViewBag.Component = await _context.Component.
                Include(m=>m.Module).
                ToListAsync();
            ViewBag.Module = await _context.Module.ToListAsync();
            return View("Component",model);
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var component= await _context.Component.FindAsync(id);
            if(component != null)
            {
                _context.Component.Remove(component);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Component");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var component = await _context.Component.FindAsync(id);
            if(component == null )
            {
                return NotFound();
            }
            ViewBag.Module = await _context.Module.ToListAsync();
            ViewBag.Component=await _context.Component.ToListAsync();
            return View("Component",component);
        }
        [HttpPost]
        public async Task<IActionResult> Create(Component component)
        {
            try
            {
                if (component.Id > 0)
                {
                    var existingComponent= await _context.Component.FindAsync(component.Id);
                    if(existingComponent != null)
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
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
