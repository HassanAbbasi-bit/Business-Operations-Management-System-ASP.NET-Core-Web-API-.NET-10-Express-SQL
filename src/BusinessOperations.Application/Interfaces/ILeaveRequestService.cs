using BusinessOperations.Application.Common.Models;
using BusinessOperations.Application.DTOs.LeaveRequests;
using BusinessOperations.Domain.Enums;

namespace BusinessOperations.Application.Interfaces;

public interface ILeaveRequestService
{
    Task<LeaveRequestDto> SubmitAsync(Guid currentUserId, CreateLeaveRequestDto request, CancellationToken cancellationToken = default);
    Task<LeaveRequestDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequestDto>> GetMyRequestsAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<PagedResult<LeaveRequestDto>> GetPagedAsync(
        LeaveStatus? status,
        Guid? employeeId,
        Guid? departmentId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<LeaveRequestDto> ApproveAsync(Guid id, Guid reviewerUserId, ReviewLeaveRequestDto? reviewDto, CancellationToken cancellationToken = default);
    Task<LeaveRequestDto> RejectAsync(Guid id, Guid reviewerUserId, string rejectionReason, CancellationToken cancellationToken = default);
    Task<LeaveRequestDto> CancelAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);
}
