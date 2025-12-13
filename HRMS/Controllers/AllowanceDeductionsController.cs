using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Models;

namespace HRMS.Controllers
{
    public class AllowanceDeductionsController : Controller
    {
        private readonly HrmsDbContext _context;

        public AllowanceDeductionsController(HrmsDbContext context)
        {
            _context = context;
        }

        // GET: AllowanceDeductions
        public async Task<IActionResult> Index()
        {
            var hrmsDbContext = _context.AllowanceDeduction.Include(a => a.currency_codeNavigation).Include(a => a.employee).Include(a => a.payroll);
            return View(await hrmsDbContext.ToListAsync());
        }

        // GET: AllowanceDeductions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var allowanceDeduction = await _context.AllowanceDeduction
                .Include(a => a.currency_codeNavigation)
                .Include(a => a.employee)
                .Include(a => a.payroll)
                .FirstOrDefaultAsync(m => m.ad_id == id);
            if (allowanceDeduction == null)
            {
                return NotFound();
            }

            return View(allowanceDeduction);
        }

        // GET: AllowanceDeductions/Create
        public IActionResult Create()
        {
            ViewData["currency_code"] = new SelectList(_context.Currency, "CurrencyCode", "CurrencyCode");
            ViewData["employee_id"] = new SelectList(_context.Employee, "employee_id", "employee_id");
            ViewData["payroll_id"] = new SelectList(_context.Payroll, "payroll_id", "payroll_id");
            return View();
        }

        // POST: AllowanceDeductions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ad_id,payroll_id,employee_id,type,amount,currency_code,duration,timezone")] AllowanceDeduction allowanceDeduction)
        {
            if (ModelState.IsValid)
            {
                _context.Add(allowanceDeduction);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["currency_code"] = new SelectList(_context.Currency, "CurrencyCode", "CurrencyCode", allowanceDeduction.currency_code);
            ViewData["employee_id"] = new SelectList(_context.Employee, "employee_id", "employee_id", allowanceDeduction.employee_id);
            ViewData["payroll_id"] = new SelectList(_context.Payroll, "payroll_id", "payroll_id", allowanceDeduction.payroll_id);
            return View(allowanceDeduction);
        }

        // GET: AllowanceDeductions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var allowanceDeduction = await _context.AllowanceDeduction.FindAsync(id);
            if (allowanceDeduction == null)
            {
                return NotFound();
            }
            ViewData["currency_code"] = new SelectList(_context.Currency, "CurrencyCode", "CurrencyCode", allowanceDeduction.currency_code);
            ViewData["employee_id"] = new SelectList(_context.Employee, "employee_id", "employee_id", allowanceDeduction.employee_id);
            ViewData["payroll_id"] = new SelectList(_context.Payroll, "payroll_id", "payroll_id", allowanceDeduction.payroll_id);
            return View(allowanceDeduction);
        }

        // POST: AllowanceDeductions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ad_id,payroll_id,employee_id,type,amount,currency_code,duration,timezone")] AllowanceDeduction allowanceDeduction)
        {
            if (id != allowanceDeduction.ad_id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(allowanceDeduction);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AllowanceDeductionExists(allowanceDeduction.ad_id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["currency_code"] = new SelectList(_context.Currency, "CurrencyCode", "CurrencyCode", allowanceDeduction.currency_code);
            ViewData["employee_id"] = new SelectList(_context.Employee, "employee_id", "employee_id", allowanceDeduction.employee_id);
            ViewData["payroll_id"] = new SelectList(_context.Payroll, "payroll_id", "payroll_id", allowanceDeduction.payroll_id);
            return View(allowanceDeduction);
        }

        // GET: AllowanceDeductions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var allowanceDeduction = await _context.AllowanceDeduction
                .Include(a => a.currency_codeNavigation)
                .Include(a => a.employee)
                .Include(a => a.payroll)
                .FirstOrDefaultAsync(m => m.ad_id == id);
            if (allowanceDeduction == null)
            {
                return NotFound();
            }

            return View(allowanceDeduction);
        }

        // POST: AllowanceDeductions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var allowanceDeduction = await _context.AllowanceDeduction.FindAsync(id);
            if (allowanceDeduction != null)
            {
                _context.AllowanceDeduction.Remove(allowanceDeduction);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AllowanceDeductionExists(int id)
        {
            return _context.AllowanceDeduction.Any(e => e.ad_id == id);
        }
    }
}
