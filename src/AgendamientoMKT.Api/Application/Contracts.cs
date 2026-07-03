using AgendamientoMKT.Api.Domain;

namespace AgendamientoMKT.Api.Application;

public sealed record LoginRequest(string Email, string Password);
public sealed record LoginResponse(string Token, DateTimeOffset ExpiresAt, UserProfile User);
public sealed record UserProfile(Guid Id, string Email, string DisplayName, Guid SiteId, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions);
public sealed record CreateUserRequest(string Email, string DisplayName, string Password, Guid SiteId, IReadOnlyCollection<Guid> RoleIds);
public sealed record CreateBookingRequest(Guid RequirementId, Guid ActivityId, Guid ServiceId, Guid SiteId, string Title, string Priority, decimal EstimatedHours);
public sealed record AddAssignmentRequest(Guid UserId, AssignmentRole Role, decimal AllocatedHours);
public sealed record AddBlockRequest(DateTimeOffset Start, DateTimeOffset End);
public sealed record ConfirmAssignmentRequest(bool Accepted, string? Comment);
public sealed record TrackUsageRequest(string EventName, string ScreenCode, int DurationMs, string? MetadataJson);
public sealed record LookupDto(Guid Id, string Code, string Name);
public sealed record MenuDto(string Code, string Label, string Route, int Order, string RequiredPermission);
public sealed record ParameterDto(Guid Id, string Group, string Key, string Value, string Description);
public sealed record UpdateParameterRequest(string Value);
public sealed record AuditDto(long Id, DateTimeOffset OccurredAt, Guid? ActorId, string Action, string EntityType, string EntityId, string DataJson);
public sealed record MetricSummaryDto(string ScreenCode, int Visits, double AverageDurationMs, int Errors);
public sealed record UserListDto(Guid Id, string Email, string DisplayName, Guid SiteId, bool IsActive, IReadOnlyCollection<string> Roles);
public sealed record BookingDto(Guid Id, Guid RequirementId, Guid ActivityId, string Title, string Priority, decimal EstimatedHours, string Status, int Version, IReadOnlyCollection<AssignmentDto> Assignments);
public sealed record AssignmentDto(Guid Id, Guid UserId, string Role, decimal AllocatedHours, string Confirmation, IReadOnlyCollection<BlockDto> Blocks);
public sealed record BlockDto(Guid Id, DateTimeOffset Start, DateTimeOffset End, decimal Hours);

public interface ICurrentUser { Guid? UserId { get; } }
public interface IClock { DateTimeOffset UtcNow { get; } }
public sealed class SystemClock : IClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }

public interface IUserRepository
{
    Task<AppUser?> FindByEmailAsync(string email, CancellationToken ct);
    Task<AppUser?> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyCollection<AppUser>> ListAsync(CancellationToken ct);
    Task AddAsync(AppUser user, CancellationToken ct);
}

public interface IBookingRepository
{
    Task<Booking?> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyCollection<Booking>> ListAsync(CancellationToken ct);
    Task AddAsync(Booking booking, CancellationToken ct);
    Task<bool> HasConflictAsync(Guid userId, DateTimeOffset start, DateTimeOffset endAt, Guid? excludedBookingId, CancellationToken ct);
}

public interface IUnitOfWork { Task<int> SaveChangesAsync(CancellationToken ct); }
public interface IAuditWriter { Task WriteAsync(string action, string entityType, string entityId, object data, CancellationToken ct); }
public interface IAdministrationRepository
{
    Task<IReadOnlyCollection<LookupDto>> RolesAsync(CancellationToken ct);
    Task<IReadOnlyDictionary<Guid, Role>> RolesByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);
    Task<IReadOnlyCollection<LookupDto>> SitesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<LookupDto>> ServicesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<MenuDto>> MenuAsync(IReadOnlyCollection<string> permissions, CancellationToken ct);
    Task<IReadOnlyCollection<ParameterDto>> ParametersAsync(CancellationToken ct);
    Task<SystemParameter?> ParameterAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyCollection<AuditDto>> AuditAsync(int take, CancellationToken ct);
    Task<IReadOnlyCollection<MetricSummaryDto>> MetricsAsync(DateTimeOffset from, CancellationToken ct);
    Task<IReadOnlyCollection<UserListDto>> UsersAsync(CancellationToken ct);
}
