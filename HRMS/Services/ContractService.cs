using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Models;

namespace HRMS.Services;

public class ContractService : IContractService
{
    private readonly HrmsDbContext _context;

    public ContractService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<Contract?> GetByIdAsync(int contractId)
    {
        return await _context.Contract
            .Include(c => c.FullTimeContract)
            .Include(c => c.PartTimeContract)
            .Include(c => c.InternshipContract)
            .Include(c => c.ConsultantContract)
            .FirstOrDefaultAsync(c => c.contract_id == contractId);
    }

    public async Task<IEnumerable<Contract>> GetAllAsync()
    {
        return await _context.Contract.ToListAsync();
    }

    public async Task<IEnumerable<Contract>> GetByEmployeeIdAsync(int employeeId)
    {
        return await _context.Contract
            .Where(c => c.Employee.Any(e => e.employee_id == employeeId))
            .ToListAsync();
    }

    public async Task<IEnumerable<Contract>> GetActiveContractsAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return await _context.Contract
            .Where(c => (c.start_date == null || c.start_date <= today) &&
                       (c.end_date == null || c.end_date >= today) &&
                       c.current_state != "Terminated")
            .ToListAsync();
    }

    public async Task<Contract> CreateAsync(Contract contract)
    {
        _context.Contract.Add(contract);
        await _context.SaveChangesAsync();
        return contract;
    }

    public async Task<Contract> UpdateAsync(Contract contract)
    {
        _context.Contract.Update(contract);
        await _context.SaveChangesAsync();
        return contract;
    }

    public async Task<bool> DeleteAsync(int contractId)
    {
        var contract = await GetByIdAsync(contractId);
        if (contract == null)
            return false;

        _context.Contract.Remove(contract);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int contractId)
    {
        return await _context.Contract
            .AnyAsync(c => c.contract_id == contractId);
    }
}

