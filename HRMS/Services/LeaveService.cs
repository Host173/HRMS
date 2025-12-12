using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Models;

namespace HRMS.Services;

public class LeaveService : ILeaveService
{
    private readonly HrmsDbContext _context;

    public LeaveService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<LeaveRequest?> GetByIdAsync(int requestId)
    {
        return await _context.LeaveRequest
            .Include(lr => lr.employee)
            .Include(lr => lr.leave)
            .Include(lr => lr.LeaveDocument)
            .FirstOrDefaultAsync(lr => lr.request_id == requestId);
    }

    public async Task<IEnumerable<LeaveRequest>> GetAllAsync()
    {
        return await _context.LeaveRequest
            .Include(lr => lr.employee)
            .Include(lr => lr.leave)
            .ToListAsync();
    }

    public async Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(int employeeId)
    {
        return await _context.LeaveRequest
            .Include(lr => lr.leave)
            .Where(lr => lr.employee_id == employeeId)
            .OrderByDescending(lr => lr.request_id)
            .ToListAsync();
    }

    public async Task<IEnumerable<LeaveRequest>> GetPendingRequestsAsync()
    {
        return await _context.LeaveRequest
            .Include(lr => lr.employee)
            .Include(lr => lr.leave)
            .Where(lr => lr.status == "Pending")
            .OrderByDescending(lr => lr.request_id)
            .ToListAsync();
    }

    public async Task<IEnumerable<LeaveRequest>> GetApprovedRequestsAsync()
    {
        return await _context.LeaveRequest
            .Include(lr => lr.employee)
            .Include(lr => lr.leave)
            .Where(lr => lr.status == "Approved")
            .OrderByDescending(lr => lr.request_id)
            .ToListAsync();
    }

    public async Task<LeaveRequest> CreateAsync(LeaveRequest leaveRequest)
    {
        _context.LeaveRequest.Add(leaveRequest);
        await _context.SaveChangesAsync();
        return leaveRequest;
    }

    public async Task<LeaveRequest> UpdateAsync(LeaveRequest leaveRequest)
    {
        _context.LeaveRequest.Update(leaveRequest);
        await _context.SaveChangesAsync();
        return leaveRequest;
    }

    public async Task<bool> DeleteAsync(int requestId)
    {
        var leaveRequest = await GetByIdAsync(requestId);
        if (leaveRequest == null)
            return false;

        _context.LeaveRequest.Remove(leaveRequest);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int requestId)
    {
        return await _context.LeaveRequest
            .AnyAsync(lr => lr.request_id == requestId);
    }

    public async Task<bool> ApproveAsync(int requestId, int approvedBy)
    {
        var leaveRequest = await GetByIdAsync(requestId);
        if (leaveRequest == null)
            return false;

        leaveRequest.status = "Approved";
        leaveRequest.approved_by = approvedBy;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectAsync(int requestId, int rejectedBy)
    {
        var leaveRequest = await GetByIdAsync(requestId);
        if (leaveRequest == null)
            return false;

        leaveRequest.status = "Rejected";
        leaveRequest.approved_by = rejectedBy;
        await _context.SaveChangesAsync();
        return true;
    }
}

