using BusinessOperations.Application.DTOs.Dashboard;

namespace BusinessOperations.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<PersonalDashboardDto?> GetPersonalSummaryAsync(Guid currentUserId, CancellationToken cancellationToken = default);
}
