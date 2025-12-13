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
    public class EmployeesController : Controller
    {
        private readonly HrmsDbContext _context;

        public EmployeesController(HrmsDbContext context)
        {
            _context = context;
        }

        // GET: Employees
        public async Task<IActionResult> Index()
        {
            var hrmsDbContext = _context.Employee.Include(e => e.contract).Include(e => e.department).Include(e => e.manager).Include(e => e.pay_grade).Include(e => e.position).Include(e => e.salary_type).Include(e => e.tax_form);
            return View(await hrmsDbContext.ToListAsync());
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employee
                .Include(e => e.contract)
                .Include(e => e.department)
                .Include(e => e.manager)
                .Include(e => e.pay_grade)
                .Include(e => e.position)
                .Include(e => e.salary_type)
                .Include(e => e.tax_form)
                .FirstOrDefaultAsync(m => m.employee_id == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // GET: Employees/Create
        public IActionResult Create()
        {
            ViewData["contract_id"] = new SelectList(_context.Contract, "contract_id", "contract_id");
            ViewData["department_id"] = new SelectList(_context.Department, "department_id", "department_id");
            ViewData["manager_id"] = new SelectList(_context.Employee, "employee_id", "employee_id");
            ViewData["pay_grade_id"] = new SelectList(_context.PayGrade, "pay_grade_id", "pay_grade_id");
            ViewData["position_id"] = new SelectList(_context.Position, "position_id", "position_id");
            ViewData["salary_type_id"] = new SelectList(_context.SalaryType, "salary_type_id", "salary_type_id");
            ViewData["tax_form_id"] = new SelectList(_context.TaxForm, "tax_form_id", "tax_form_id");
            return View();
        }

        // POST: Employees/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("employee_id,first_name,last_name,full_name,national_id,date_of_birth,country_of_birth,phone,email,address,emergency_contact_name,emergency_contact_phone,relationship,biography,profile_image,employment_progress,account_status,employment_status,hire_date,is_active,profile_completion,department_id,position_id,manager_id,contract_id,tax_form_id,salary_type_id,pay_grade_id,password_hash")] Employee employee)
        {
            if (ModelState.IsValid)
            {
                _context.Add(employee);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["contract_id"] = new SelectList(_context.Contract, "contract_id", "contract_id", employee.contract_id);
            ViewData["department_id"] = new SelectList(_context.Department, "department_id", "department_id", employee.department_id);
            ViewData["manager_id"] = new SelectList(_context.Employee, "employee_id", "employee_id", employee.manager_id);
            ViewData["pay_grade_id"] = new SelectList(_context.PayGrade, "pay_grade_id", "pay_grade_id", employee.pay_grade_id);
            ViewData["position_id"] = new SelectList(_context.Position, "position_id", "position_id", employee.position_id);
            ViewData["salary_type_id"] = new SelectList(_context.SalaryType, "salary_type_id", "salary_type_id", employee.salary_type_id);
            ViewData["tax_form_id"] = new SelectList(_context.TaxForm, "tax_form_id", "tax_form_id", employee.tax_form_id);
            return View(employee);
        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employee.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            ViewData["contract_id"] = new SelectList(_context.Contract, "contract_id", "contract_id", employee.contract_id);
            ViewData["department_id"] = new SelectList(_context.Department, "department_id", "department_id", employee.department_id);
            ViewData["manager_id"] = new SelectList(_context.Employee, "employee_id", "employee_id", employee.manager_id);
            ViewData["pay_grade_id"] = new SelectList(_context.PayGrade, "pay_grade_id", "pay_grade_id", employee.pay_grade_id);
            ViewData["position_id"] = new SelectList(_context.Position, "position_id", "position_id", employee.position_id);
            ViewData["salary_type_id"] = new SelectList(_context.SalaryType, "salary_type_id", "salary_type_id", employee.salary_type_id);
            ViewData["tax_form_id"] = new SelectList(_context.TaxForm, "tax_form_id", "tax_form_id", employee.tax_form_id);
            return View(employee);
        }

        // POST: Employees/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("employee_id,first_name,last_name,full_name,national_id,date_of_birth,country_of_birth,phone,email,address,emergency_contact_name,emergency_contact_phone,relationship,biography,profile_image,employment_progress,account_status,employment_status,hire_date,is_active,profile_completion,department_id,position_id,manager_id,contract_id,tax_form_id,salary_type_id,pay_grade_id,password_hash")] Employee employee)
        {
            if (id != employee.employee_id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(employee);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.employee_id))
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
            ViewData["contract_id"] = new SelectList(_context.Contract, "contract_id", "contract_id", employee.contract_id);
            ViewData["department_id"] = new SelectList(_context.Department, "department_id", "department_id", employee.department_id);
            ViewData["manager_id"] = new SelectList(_context.Employee, "employee_id", "employee_id", employee.manager_id);
            ViewData["pay_grade_id"] = new SelectList(_context.PayGrade, "pay_grade_id", "pay_grade_id", employee.pay_grade_id);
            ViewData["position_id"] = new SelectList(_context.Position, "position_id", "position_id", employee.position_id);
            ViewData["salary_type_id"] = new SelectList(_context.SalaryType, "salary_type_id", "salary_type_id", employee.salary_type_id);
            ViewData["tax_form_id"] = new SelectList(_context.TaxForm, "tax_form_id", "tax_form_id", employee.tax_form_id);
            return View(employee);
        }

        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employee
                .Include(e => e.contract)
                .Include(e => e.department)
                .Include(e => e.manager)
                .Include(e => e.pay_grade)
                .Include(e => e.position)
                .Include(e => e.salary_type)
                .Include(e => e.tax_form)
                .FirstOrDefaultAsync(m => m.employee_id == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employee.FindAsync(id);
            if (employee != null)
            {
                _context.Employee.Remove(employee);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employee.Any(e => e.employee_id == id);
        }
    }
}
