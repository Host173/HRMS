using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using HRMS.Data;
using HRMS.Helpers;
using HRMS.Models;

namespace HRMS.Services;

public class LeaveService : ILeaveService
{
    private readonly HrmsDbContext _context;

    public LeaveService(HrmsDbContext context)
    {
        _context = context;
    }

    private async Task<(DbConnection Connection, bool OpenedHere)> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
        {
            await connection.OpenAsync(cancellationToken);
        }
        return (connection, openedHere);
    }

    private static async Task CloseIfNeededAsync(DbConnection connection, bool openedHere, CancellationToken cancellationToken = default)
    {
        if (openedHere && connection.State == ConnectionState.Open)
        {
            await connection.CloseAsync();
        }
    }

    private static DbParameter CreateParameter(DbCommand command, string name, DbType type, object? value, int? size = null)
    {
        var p = command.CreateParameter();
        p.ParameterName = name;
        p.DbType = type;
        p.Value = value ?? DBNull.Value;
        if (size.HasValue)
        {
            p.Size = size.Value;
        }
        return p;
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
        // Primary list comes from existing stored procedure ViewLeaveHistory.
        // We then hydrate full entities (including dates/docs) from the table so views can display them.
        var requestIds = new List<int>();

        var (connection, openedHere) = await GetOpenConnectionAsync();
        try
        {
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "ViewLeaveHistory";
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(CreateParameter(command, "@EmployeeID", DbType.Int32, employeeId));

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    if (!reader.IsDBNull(0))
                    {
                        requestIds.Add(reader.GetInt32(0)); // request_id
                    }
                }
            }
        }
        finally
        {
            await CloseIfNeededAsync(connection, openedHere);
        }

        if (requestIds.Count == 0)
        {
            return Array.Empty<LeaveRequest>();
        }

        return await _context.LeaveRequest
            .Include(lr => lr.employee)
            .Include(lr => lr.leave)
            .Include(lr => lr.LeaveDocument)
            .Where(lr => requestIds.Contains(lr.request_id))
            .OrderByDescending(lr => lr.request_id)
            .ToListAsync();
    }

    public async Task<IEnumerable<LeaveRequest>> GetByManagerIdAsync(int managerId)
    {
        // Manager team queue comes from existing stored procedure GetPendingLeaveRequests.
        // We then hydrate entities for the UI.
        var requestIds = new List<int>();

        var (connection, openedHere) = await GetOpenConnectionAsync();
        try
        {
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "GetPendingLeaveRequests";
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(CreateParameter(command, "@ManagerID", DbType.Int32, managerId));

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var ordinal = reader.GetOrdinal("request_id");
                    if (!reader.IsDBNull(ordinal))
                    {
                        requestIds.Add(reader.GetInt32(ordinal));
                    }
                }
            }
        }
        finally
        {
            await CloseIfNeededAsync(connection, openedHere);
        }

        if (requestIds.Count == 0)
        {
            return Array.Empty<LeaveRequest>();
        }

        return await _context.LeaveRequest
            .Include(lr => lr.employee)
            .Include(lr => lr.leave)
            .Include(lr => lr.LeaveDocument)
            .Where(lr => requestIds.Contains(lr.request_id))
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

    public async Task<Dictionary<string, LeaveBalanceViewModel>> GetLeaveBalanceAsync(int employeeId)
    {
        var result = new Dictionary<string, LeaveBalanceViewModel>(StringComparer.OrdinalIgnoreCase);

        var (connection, openedHere) = await GetOpenConnectionAsync();
        try
        {
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "GetLeaveBalance";
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(CreateParameter(command, "@EmployeeID", DbType.Int32, employeeId));

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var leaveType = Convert.ToString(reader["leave_type"]) ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(leaveType))
                        continue;

                    var total = Convert.ToDecimal(reader["TotalEntitlement"]);
                    var used = Convert.ToDecimal(reader["UsedDays"]);
                    var remaining = Convert.ToDecimal(reader["RemainingBalance"]);

                    result[leaveType] = new LeaveBalanceViewModel
                    {
                        LeaveType = leaveType,
                        TotalEntitlement = total,
                        Used = used,
                        Remaining = remaining
                    };
                }
            }
        }
        finally
        {
            await CloseIfNeededAsync(connection, openedHere);
        }

        return result;
    }

    public async Task<LeaveRequest> CreateAsync(LeaveRequest leaveRequest)
    {
        // Stored procedure is the source of truth for inserts.
        string? confirmationMessage = null;

        var (connection, openedHere) = await GetOpenConnectionAsync();
        try
        {
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "SubmitLeaveRequest";
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(CreateParameter(command, "@EmployeeID", DbType.Int32, leaveRequest.employee_id));
                command.Parameters.Add(CreateParameter(command, "@LeaveTypeID", DbType.Int32, leaveRequest.leave_id));
                command.Parameters.Add(CreateParameter(command, "@StartDate", DbType.Date, leaveRequest.start_date?.ToDateTime(TimeOnly.MinValue)));
                command.Parameters.Add(CreateParameter(command, "@EndDate", DbType.Date, leaveRequest.end_date?.ToDateTime(TimeOnly.MinValue)));
                // DB stored procedure parameter is VARCHAR(100)
                var reason = leaveRequest.justification ?? string.Empty;
                if (reason.Length > 100)
                {
                    reason = reason.Substring(0, 100);
                }
                command.Parameters.Add(CreateParameter(command, "@Reason", DbType.String, reason, size: 100));

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync() && reader.FieldCount > 0)
                {
                    confirmationMessage = reader.GetValue(0)?.ToString();
                }
            }
        }
        finally
        {
            await CloseIfNeededAsync(connection, openedHere);
        }

        if (!string.IsNullOrWhiteSpace(confirmationMessage) &&
            confirmationMessage.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(confirmationMessage);
        }

        // The procedure doesn't return the new identity; fetch the latest request for this employee.
        var created = await _context.LeaveRequest
            .Where(lr => lr.employee_id == leaveRequest.employee_id && lr.leave_id == leaveRequest.leave_id)
            .OrderByDescending(lr => lr.request_id)
            .FirstOrDefaultAsync();

        if (created == null)
        {
            throw new InvalidOperationException("Leave request was submitted but could not be loaded.");
        }

        // Ensure UI-required columns are populated (the proc only sets core columns).
        created.start_date = leaveRequest.start_date;
        created.end_date = leaveRequest.end_date;
        created.created_at ??= leaveRequest.created_at ?? DateTime.UtcNow;
        created.is_irregular = leaveRequest.is_irregular;
        created.irregularity_reason = leaveRequest.irregularity_reason;

        await _context.SaveChangesAsync();

        // Important: propagate identity back to the caller object (controllers use it for attachments).
        leaveRequest.request_id = created.request_id;
        return created;
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
        // For line managers, validate the request belongs to their team (SP-level validation).
        var isHrAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, approvedBy) ||
                        await AuthorizationHelper.IsSystemAdminAsync(_context, approvedBy);

        if (!isHrAdmin)
        {
            var canView = await CanManagerViewRequestAsync(requestId, approvedBy);
            if (!canView)
                return false;
        }

        await ExecuteApproveProcedureAsync(requestId, approvedBy, status: "Approved");

        // Requirement: approved leave must sync with attendance.
        await ExecuteNonQueryProcedureAsync("SyncLeaveToAttendance", cmd =>
        {
            cmd.Parameters.Add(CreateParameter(cmd, "@LeaveRequestID", DbType.Int32, requestId));
        });

        return true;
    }

    public async Task<bool> RejectAsync(int requestId, int rejectedBy)
    {
        // For line managers, validate the request belongs to their team (SP-level validation).
        var isHrAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, rejectedBy) ||
                        await AuthorizationHelper.IsSystemAdminAsync(_context, rejectedBy);

        if (!isHrAdmin)
        {
            var canView = await CanManagerViewRequestAsync(requestId, rejectedBy);
            if (!canView)
                return false;
        }

        // Use the same procedure that updates status + approved_by.
        await ExecuteApproveProcedureAsync(requestId, rejectedBy, status: "Rejected");
        return true;
    }

    public async Task<bool> FlagAsIrregularAsync(int requestId, int managerId, string irregularityReason)
    {
        var request = await _context.LeaveRequest
            .FirstOrDefaultAsync(lr => lr.request_id == requestId);
        if (request == null)
        {
            return false;
        }

        var isHrAdmin = await AuthorizationHelper.IsHRAdminAsync(_context, managerId) ||
                        await AuthorizationHelper.IsSystemAdminAsync(_context, managerId);

        if (!isHrAdmin)
        {
            // Stored procedure validates employee-manager relationship.
            await ExecuteNonQueryProcedureAsync("FlagIrregularLeave", cmd =>
            {
                cmd.Parameters.Add(CreateParameter(cmd, "@EmployeeID", DbType.Int32, request.employee_id));
                cmd.Parameters.Add(CreateParameter(cmd, "@ManagerID", DbType.Int32, managerId));
                cmd.Parameters.Add(CreateParameter(cmd, "@PatternDescription", DbType.String, irregularityReason, size: 200));
            });
        }

        // Persist UI-facing irregular flags on the specific request.
        request.is_irregular = true;
        request.irregularity_reason = irregularityReason;
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<bool> CanManagerViewRequestAsync(int requestId, int managerId)
    {
        var (connection, openedHere) = await GetOpenConnectionAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "ViewLeaveRequest";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(CreateParameter(command, "@LeaveRequestID", DbType.Int32, requestId));
            command.Parameters.Add(CreateParameter(command, "@ManagerID", DbType.Int32, managerId));

            await using var reader = await command.ExecuteReaderAsync();
            // If validation fails, the procedure returns no rowset.
            return await reader.ReadAsync();
        }
        finally
        {
            await CloseIfNeededAsync(connection, openedHere);
        }
    }

    private async Task ExecuteApproveProcedureAsync(int requestId, int approverId, string status)
    {
        await ExecuteNonQueryProcedureAsync("ApproveLeaveRequest", cmd =>
        {
            cmd.Parameters.Add(CreateParameter(cmd, "@LeaveRequestID", DbType.Int32, requestId));
            cmd.Parameters.Add(CreateParameter(cmd, "@ApproverID", DbType.Int32, approverId));
            cmd.Parameters.Add(CreateParameter(cmd, "@Status", DbType.String, status, size: 20));
        });
    }

    private async Task ExecuteNonQueryProcedureAsync(string procedureName, Action<DbCommand> configure)
    {
        var (connection, openedHere) = await GetOpenConnectionAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = procedureName;
            command.CommandType = CommandType.StoredProcedure;
            configure(command);
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            await CloseIfNeededAsync(connection, openedHere);
        }
    }
}

