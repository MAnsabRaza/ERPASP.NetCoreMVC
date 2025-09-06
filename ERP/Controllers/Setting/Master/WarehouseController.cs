using ERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Printing;

namespace ERP.Controllers.Setting.Master
{
    public class WarehouseController : Controller
    {
        private readonly AppDbContext _context;
        public WarehouseController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Warehouse(string searchString, int page = 1, int pageSize = 5)
        {
            var query = _context.Warehouse.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(w => w.warehouse_name.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var warehouseList = await query.
                Include(c => c.Company).
                OrderBy(w => w.Id).
                Skip((page - 1) * pageSize).
                Take(pageSize).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            var model = new Warehouse
            {
                current_date = DateOnly.FromDateTime(DateTime.Now)
            };
            ViewBag.companyList = await _context.Company.ToListAsync();
            ViewBag.Warehouse = warehouseList;

            return View("~/Views/Setting/Master/Warehouse.cshtml", model);
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var warehouse = await _context.Warehouse.FindAsync(id);
            if (warehouse != null)
            {
                _context.Warehouse.Remove(warehouse);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Warehouse");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string searchString, int page = 1, int pageSize = 5)
        {
            var warehouse = await _context.Warehouse.FindAsync(id);
            if (warehouse == null)
            {
                return NotFound();
            }
            var query = _context.Warehouse.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(w => w.warehouse_name.Contains(searchString));
            }
            var totalItems = await query.CountAsync();
            var warehouseList = await query.
                Include(c => c.Company).
            OrderBy(w => w.Id).
                Skip((page - 1) * pageSize).
                Take(pageSize).
                ToListAsync();
            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            ViewBag.companyList = await _context.Company.ToListAsync();
            ViewBag.Warehouse = warehouseList;

            //return View("Warehouse", warehouse);
            return View("~/Views/Setting/Master/Warehouse.cshtml", warehouse);

        }
        [HttpPost]
        public async Task<IActionResult> Create(Warehouse warehouse)
        {
            try
            {
                if (warehouse.Id > 0)
                {
                    var existingWarehouse = await _context.Warehouse.FindAsync(warehouse.Id);
                    if (existingWarehouse != null)
                    {
                        existingWarehouse.current_date = warehouse.current_date;
                        existingWarehouse.warehouse_name = warehouse.warehouse_name;
                        existingWarehouse.warehouse_description = warehouse.warehouse_description;
                        existingWarehouse.address = warehouse.address;
                        existingWarehouse.city = warehouse.city;
                        existingWarehouse.status = warehouse.status;
                        existingWarehouse.type = warehouse.type;
                        existingWarehouse.companyId = warehouse.companyId;
                        _context.Update(existingWarehouse);
                        await _context.SaveChangesAsync();
                    }

                }
                else
                {
                    _context.Warehouse.Add(warehouse);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("Warehouse");
            }
            catch (Exception ex)
            {
                return RedirectToAction(ex.Message);
            }
        }
    }
}
