using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Models;
using HRMS.Services;
using HRMS.Helpers;

namespace HRMS.Controllers;

[Authorize]
public class ContractController : Controller
{
    private readonly IContractService _contractService;
    private readonly IEmployeeService _employeeService;
    private readonly INotificationService _notificationService;
    private readonly HrmsDbContext _context;
    private readonly ILogger<ContractController> _logger;

    public ContractController(
        IContractService contractService,
        IEmployeeService employeeService,
        INotificationService notificationService,
        HrmsDbContext context,
        ILogger<ContractController> logger)
    {
        _contractService = contractService;
        _employeeService = employeeService;
        _notificationService = notificationService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Maps display contract type to database format (matches CHECK constraint: 'permanent','temporary','consultant','internship')
    /// </summary>
    private static string MapContractTypeToDatabase(string displayType)
    {
        return displayType switch
        {
            "Full-Time" => "permanent",
            "Part-Time" => "temporary",
            "Internship" => "internship",
            "Consultant" => "consultant",
            // Handle if already in database format
            "permanent" => "permanent",
            "temporary" => "temporary",
            "internship" => "internship",
            "consultant" => "consultant",
            // Handle old formats if any
            "FullTime" => "permanent",
            "PartTime" => "temporary",
            _ => displayType?.ToLower() ?? "permanent" // Default to permanent if unknown
        };
    }

    /// <summary>
    /// Maps database contract type to display format (adds hyphens and proper casing)
    /// </summary>
    private static string MapContractTypeToDisplay(string dbType)
    {
        return dbType?.ToLower() switch
        {
            "permanent" => "Full-Time",
            "temporary" => "Part-Time",
            "internship" => "Internship",
            "consultant" => "Consultant",
            // Handle if already in display format
            "full-time" => "Full-Time",
            "part-time" => "Part-Time",
            _ => dbType ?? "Full-Time" // Default to Full-Time if unknown
        };
    }

    /// <summary>
    /// List all contracts - HR Admins can see all, others see their own
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!currentEmployeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var isHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, currentEmployeeId.Value);

        IEnumerable<Contract> contracts;

        if (isHRAdmin)
        {
            contracts = await _context.Contract
                .Include(c => c.Employee)
                .OrderByDescending(c => c.start_date)
                .ToListAsync();
        }
        else
        {
            contracts = await _contractService.GetByEmployeeIdAsync(currentEmployeeId.Value);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        // Separate active and expiring contracts
        var activeContracts = contracts.Where(c => 
            c.current_state == "Active" && 
            (!c.end_date.HasValue || c.end_date.Value >= today))
            .ToList();

        var expiringContracts = contracts.Where(c =>
            c.current_state == "Active" &&
            c.end_date.HasValue &&
            c.end_date.Value >= today &&
            c.end_date.Value <= today.AddDays(30))
            .ToList();

        // Get expired contracts (end date has passed and status is still Active)
        var expiredContracts = contracts.Where(c =>
            c.end_date.HasValue &&
            c.end_date.Value < today &&
            (c.current_state == "Active" || c.current_state == null || c.current_state == "Draft"))
            .ToList();

        ViewBag.ActiveContracts = activeContracts;
        ViewBag.ExpiringContracts = expiringContracts;
        ViewBag.ExpiredContracts = expiredContracts;
        ViewBag.IsHRAdmin = isHRAdmin;

        return View(contracts);
    }

    /// <summary>
    /// View contract details
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var contract = await _contractService.GetByIdAsync(id);
        if (contract == null)
        {
            return NotFound();
        }

        var currentEmployeeId = AuthorizationHelper.GetCurrentEmployeeId(User);
        if (!currentEmployeeId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        // Check if user has access to this contract
        var isHRAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, currentEmployeeId.Value);
        var isContractOwner = contract.Employee.Any(e => e.employee_id == currentEmployeeId.Value);

        if (!isHRAdmin && !isContractOwner)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        await _context.Entry(contract)
            .Collection(c => c.Employee)
            .LoadAsync();

        ViewBag.IsHRAdmin = isHRAdmin;

        return View(contract);
    }

    /// <summary>
    /// HR Admins can create employment contracts for employees
    /// </summary>
    [HttpGet]
    [RequireRole(AuthorizationHelper.HRAdminRole)]
    public async Task<IActionResult> Create(int? employeeId = null)
    {
        ViewBag.Employees = await _context.Employee
            .Where(e => e.is_active == true)
            .OrderBy(e => e.full_name)
            .ToListAsync();

        ViewBag.ContractTypes = new List<string> { "Full-Time", "Part-Time", "Internship", "Consultant" };

        var contract = new Contract
        {
            current_state = "Draft",
            start_date = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        if (employeeId.HasValue)
        {
            ViewBag.SelectedEmployeeId = employeeId.Value;
        }

        return View(contract);
    }

    [HttpPost]
    [RequireRole(AuthorizationHelper.HRAdminRole)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Contract contract, int employeeId)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Employees = await _context.Employee
                .Where(e => e.is_active == true)
                .OrderBy(e => e.full_name)
                .ToListAsync();
            ViewBag.ContractTypes = new List<string> { "Full-Time", "Part-Time", "Internship", "Consultant" };
            return View(contract);
        }

        // Validate employee exists
        var employee = await _employeeService.GetByIdAsync(employeeId);
        if (employee == null)
        {
            ModelState.AddModelError(string.Empty, "Selected employee not found.");
            ViewBag.Employees = await _context.Employee
                .Where(e => e.is_active == true)
                .OrderBy(e => e.full_name)
                .ToListAsync();
            ViewBag.ContractTypes = new List<string> { "Full-Time", "Part-Time", "Internship", "Consultant" };
            return View(contract);
        }

        // Check if employee already has a contract - we'll allow multiple contracts but warn
        // The contract_id field can be null, so we can create contracts without assigning to employee.contract_id
        // Instead, we'll use the Contract.Employee collection relationship

        // Validate required fields
        if (string.IsNullOrEmpty(contract.type))
        {
            ModelState.AddModelError(nameof(contract.type), "Contract type is required.");
        }

        if (!contract.start_date.HasValue)
        {
            ModelState.AddModelError(nameof(contract.start_date), "Start date is required.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Employees = await _context.Employee
                .Where(e => e.is_active == true)
                .OrderBy(e => e.full_name)
                .ToListAsync();
            ViewBag.ContractTypes = new List<string> { "Full-Time", "Part-Time", "Internship", "Consultant" };
            return View(contract);
        }

        // Set default state if not set
        if (string.IsNullOrEmpty(contract.current_state))
        {
            contract.current_state = "Active";
        }

        // Map display values to database values (CHECK constraint expects values without hyphens)
        if (!string.IsNullOrEmpty(contract.type))
        {
            contract.type = MapContractTypeToDatabase(contract.type);
        }

        try
        {
            // Create the contract first
            var createdContract = await _contractService.CreateAsync(contract);

            // Update employee's contract_id only if they don't already have one
            // This allows employees to have a primary contract reference
            // Multiple contracts can still exist through the Contract.Employee collection
            if (!employee.contract_id.HasValue)
            {
                employee.contract_id = createdContract.contract_id;
                try
                {
                    await _employeeService.UpdateAsync(employee);
                }
                catch (System.Exception updateEx)
                {
                    _logger.LogWarning(updateEx, "Could not update employee contract_id, but contract was created successfully");
                    // Contract is still created, just the employee reference wasn't updated
                }
            }
            else
            {
                _logger.LogInformation("Employee {EmployeeId} already has contract_id {ContractId}, new contract {NewContractId} created but not set as primary", 
                    employeeId, employee.contract_id, createdContract.contract_id);
            }

            // Create notification for contract creation
            try
            {
                await _notificationService.CreateNotificationAsync(
                    employeeId,
                    "Contract Created",
                    $"A new {contract.type} contract has been created for you. Start Date: {contract.start_date}",
                    "Contract",
                    "Normal"
                );
            }
            catch (System.Exception notifEx)
            {
                _logger.LogWarning(notifEx, "Failed to create notification for contract, but contract was created successfully");
            }

            _logger.LogInformation("Contract created for employee {EmployeeId} by HR Admin", employeeId);
            TempData["SuccessMessage"] = "Contract created successfully and employee has been notified.";
            return RedirectToAction(nameof(Details), new { id = createdContract.contract_id });
        }
        catch (System.Exception ex)
        {
            var errorMessage = ex.Message;
            if (ex.InnerException != null)
            {
                errorMessage += $" Inner: {ex.InnerException.Message}";
            }
            
            _logger.LogError(ex, "Error creating contract for employee {EmployeeId}. Error: {Error}", employeeId, errorMessage);
            
            // Check if employee already has a contract
            if (employee.contract_id.HasValue)
            {
                ModelState.AddModelError(string.Empty, $"Employee already has an active contract (Contract ID: {employee.contract_id}). Please update the existing contract instead.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, $"An error occurred while creating the contract: {errorMessage}");
            }
            
            ViewBag.Employees = await _context.Employee
                .Where(e => e.is_active == true)
                .OrderBy(e => e.full_name)
                .ToListAsync();
            ViewBag.ContractTypes = new List<string> { "Full-Time", "Part-Time", "Internship", "Consultant" };
            return View(contract);
        }
    }

    /// <summary>
    /// HR Admins can renew or update expiring contracts
    /// </summary>
    [HttpGet]
    [RequireRole(AuthorizationHelper.HRAdminRole)]
    public async Task<IActionResult> Renew(int id)
    {
        var contract = await _contractService.GetByIdAsync(id);
        if (contract == null)
        {
            return NotFound();
        }

        await _context.Entry(contract)
            .Collection(c => c.Employee)
            .LoadAsync();

        ViewBag.Employees = await _context.Employee
            .Where(e => e.is_active == true)
            .OrderBy(e => e.full_name)
            .ToListAsync();

        ViewBag.ContractTypes = new List<string> { "Full-Time", "Part-Time", "Internship", "Consultant" };

        // Create a new contract based on the existing one
        var newContract = new Contract
        {
            type = contract.type,
            start_date = contract.end_date?.AddDays(1) ?? DateOnly.FromDateTime(DateTime.UtcNow),
            current_state = "Draft"
        };

        ViewBag.OriginalContractId = id;
        ViewBag.EmployeeId = contract.Employee.FirstOrDefault()?.employee_id;

        return View("Create", newContract);
    }

    /// <summary>
    /// HR Admins can update contracts
    /// </summary>
    [HttpGet]
    [RequireRole(AuthorizationHelper.HRAdminRole)]
    public async Task<IActionResult> Edit(int id)
    {
        var contract = await _contractService.GetByIdAsync(id);
        if (contract == null)
        {
            return NotFound();
        }

        await _context.Entry(contract)
            .Collection(c => c.Employee)
            .LoadAsync();

        ViewBag.Employees = await _context.Employee
            .Where(e => e.is_active == true)
            .OrderBy(e => e.full_name)
            .ToListAsync();

        ViewBag.ContractTypes = new List<string> { "Full-Time", "Part-Time", "Internship", "Consultant" };
        ViewBag.ContractStates = new List<string> { "Draft", "Active", "Expired", "Terminated" };

        // Map database format to display format for the view
        if (!string.IsNullOrEmpty(contract.type))
        {
            contract.type = MapContractTypeToDisplay(contract.type);
        }

        return View(contract);
    }

    [HttpPost]
    [RequireRole(AuthorizationHelper.HRAdminRole)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Contract contract)
    {
        if (id != contract.contract_id)
        {
            return NotFound();
        }

        var existingContract = await _contractService.GetByIdAsync(id);
        if (existingContract == null)
        {
            return NotFound();
        }

        await _context.Entry(existingContract)
            .Collection(c => c.Employee)
            .LoadAsync();

        // Map display values to database values before updating
        if (!string.IsNullOrEmpty(contract.type))
        {
            contract.type = MapContractTypeToDatabase(contract.type);
        }

        // Track changes for notification
        var stateChanged = existingContract.current_state != contract.current_state;
        var endDateChanged = existingContract.end_date != contract.end_date;

        // Update contract
        existingContract.type = contract.type;
        existingContract.start_date = contract.start_date;
        existingContract.end_date = contract.end_date;
        existingContract.current_state = contract.current_state;

        try
        {
            await _contractService.UpdateAsync(existingContract);

            // Send notifications to employees if contract was updated
            foreach (var employee in existingContract.Employee)
            {
                var notificationMessage = "Your contract has been updated.";
                
                if (stateChanged)
                {
                    notificationMessage += $" Status changed to: {contract.current_state}";
                }
                
                if (endDateChanged && contract.end_date.HasValue)
                {
                    notificationMessage += $" New end date: {contract.end_date.Value}";
                }

                await _notificationService.CreateNotificationAsync(
                    employee.employee_id,
                    "Contract Updated",
                    notificationMessage,
                    "Contract"
                );
            }

            _logger.LogInformation("Contract {ContractId} updated by HR Admin", id);
            TempData["SuccessMessage"] = "Contract updated successfully and employees have been notified.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error updating contract {ContractId}", id);
            ModelState.AddModelError(string.Empty, "An error occurred while updating the contract.");
            
            ViewBag.Employees = await _context.Employee
                .Where(e => e.is_active == true)
                .OrderBy(e => e.full_name)
                .ToListAsync();
            ViewBag.ContractTypes = new List<string> { "Full-Time", "Part-Time", "Internship", "Consultant" };
            ViewBag.ContractStates = new List<string> { "Draft", "Active", "Expired", "Terminated" };
            
            return View(existingContract);
        }
    }

    /// <summary>
    /// Get contracts expiring soon (for dashboard/widget)
    /// </summary>
    [HttpGet]
    [RequireRole(AuthorizationHelper.HRAdminRole)]
    public async Task<IActionResult> ExpiringSoon()
    {
        var thirtyDaysFromNow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var expiringContracts = await _context.Contract
            .Include(c => c.Employee)
            .Where(c => c.current_state == "Active" &&
                       c.end_date.HasValue &&
                       c.end_date.Value >= today &&
                       c.end_date.Value <= thirtyDaysFromNow)
            .OrderBy(c => c.end_date)
            .ToListAsync();

        return View(expiringContracts);
    }
}

