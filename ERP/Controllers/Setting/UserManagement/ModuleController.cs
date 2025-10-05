using AspNetCoreHero.ToastNotification.Abstractions;
using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Printing;

namespace ERP.Controllers.Setting.UserManagement
{
    public class ModuleController : Controller
    {
        private readonly AppDbContext _content;
        private readonly INotyfService _notyf;
        public ModuleController(AppDbContext content,INotyfService notyf)
        {
            _notyf = notyf;
            _content = content;
        }
        public async Task<IActionResult> Module(string searchString, int page = 1, int pageSize = 5)
        {
            var query = _content.Module.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(m => m.module_name.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var moduleList = await query.
                OrderBy(m => m.Id).
                Skip((page - 1) * pageSize).
                Take(pageSize).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;

            var model = new Module
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            ViewBag.Module = moduleList;
            return View("~/Views/Setting/UserManagement/Module.cshtml", model);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string searchString, int page = 1, int pageSize = 5)
        {
            var module = await _content.Module.FindAsync(id);
            if (module == null)
            {
                return NotFound();
            }
            var query = _content.Module.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(m => m.module_name.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var moduleList = await query.
            OrderBy(m => m.Id).
                Skip((page - 1) * pageSize).
                Take(pageSize).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;

            ViewBag.Module = moduleList;
            return View("~/Views/Setting/UserManagement/Module.cshtml", module);
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var module = await _content.Module.FindAsync(id);
            if (module != null)
            {
                _content.Module.Remove(module);
                await _content.SaveChangesAsync();
                _notyf.Success("Module Delete Successfully");
            }
            return RedirectToAction("Module");
        }
        [HttpPost]
        public async Task<IActionResult> Create(Module module)
        {
            try
            {
                if (module.Id > 0)
                {
                    var existingModule = await _content.Module.FindAsync(module.Id);
                    if (existingModule != null)
                    {
                        existingModule.current_date = module.current_date;
                        existingModule.module_name = module.module_name;
                        existingModule.module_icon = module.module_icon;
                        existingModule.moduel_href = module.moduel_href;
                        existingModule.status = module.status;
                        _content.Update(existingModule);
                        await _content.SaveChangesAsync();
                        _notyf.Success("Module Update Successfully");
                    }
                }
                else
                {
                    _content.Module.Add(module);
                    await _content.SaveChangesAsync();
                    _notyf.Success("Module Create Successfully");
                }
                return RedirectToAction("Module");

            }
            catch (Exception ex)
            {
                _notyf.Error($"An Error Occurred: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }
    }
}
