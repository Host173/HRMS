using HRMS.Models;

namespace HRMS.Services;

public interface IContractService
{
    Task<Contract?> GetByIdAsync(int contractId);
    Task<IEnumerable<Contract>> GetAllAsync();
    Task<IEnumerable<Contract>> GetByEmployeeIdAsync(int employeeId);
    Task<IEnumerable<Contract>> GetActiveContractsAsync();
    Task<Contract> CreateAsync(Contract contract);
    Task<Contract> UpdateAsync(Contract contract);
    Task<bool> DeleteAsync(int contractId);
    Task<bool> ExistsAsync(int contractId);
}

