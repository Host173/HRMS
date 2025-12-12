using HRMS.Models;

namespace HRMS.Services;

public interface ILeaveService
{
    Task<LeaveRequest?> GetByIdAsync(int requestId);
    Task<IEnumerable<LeaveRequest>> GetAllAsync();
    Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(int employeeId);
    Task<IEnumerable<LeaveRequest>> GetPendingRequestsAsync();
    Task<IEnumerable<LeaveRequest>> GetApprovedRequestsAsync();
    Task<LeaveRequest> CreateAsync(LeaveRequest leaveRequest);
    Task<LeaveRequest> UpdateAsync(LeaveRequest leaveRequest);
    Task<bool> DeleteAsync(int requestId);
    Task<bool> ExistsAsync(int requestId);
    Task<bool> ApproveAsync(int requestId, int approvedBy);
    Task<bool> RejectAsync(int requestId, int rejectedBy);
}

