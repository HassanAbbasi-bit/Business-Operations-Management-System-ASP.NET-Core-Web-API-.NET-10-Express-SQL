using BusinessOperations.Application.Common.Models;
using BusinessOperations.Domain.Entities;
using BusinessOperations.Domain.Enums;

namespace BusinessOperations.Application.Interfaces.Repositories;

public interface ILeaveRequestRepository
{
    Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequest>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<PagedResult<LeaveRequest>> GetPagedAsync(
        LeaveStatus? status,
        Guid? employeeId,
        Guid? departmentId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<bool> HasActiveOverlappingRequestAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? excludeRequestId = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default);
    void Update(LeaveRequest leaveRequest);
}
