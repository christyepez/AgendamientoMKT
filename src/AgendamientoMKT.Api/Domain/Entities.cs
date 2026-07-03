namespace AgendamientoMKT.Api.Domain;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; protected set; } = DateTimeOffset.UtcNow;
    public bool IsActive { get; protected set; } = true;
    public void Deactivate() { IsActive = false; Touch(); }
    protected void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}

public sealed class AppUser : Entity
{
    private AppUser() { }
    public AppUser(string email, string displayName, Guid siteId)
    {
        Email = NormalizeEmail(email);
        DisplayName = Required(displayName, nameof(displayName));
        SiteId = siteId == Guid.Empty ? throw new ArgumentException("Site is required.") : siteId;
    }
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public Guid SiteId { get; private set; }
    public ICollection<UserRole> Roles { get; } = [];
    public void SetPasswordHash(string value) { PasswordHash = Required(value, nameof(value)); Touch(); }
    private static string NormalizeEmail(string value) => Required(value, nameof(value)).Trim().ToLowerInvariant();
    private static string Required(string value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"{name} is required.") : value.Trim();
}

public sealed class Role : Entity
{
    private Role() { }
    public Role(string code, string name) { Code = code.Trim().ToUpperInvariant(); Name = name.Trim(); }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public ICollection<UserRole> Users { get; } = [];
    public ICollection<RoleAccess> Permissions { get; } = [];
}

public sealed class AccessRight : Entity
{
    private AccessRight() { }
    public AccessRight(string code, string description) { Code = code.Trim().ToUpperInvariant(); Description = description.Trim(); }
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
}

public sealed class UserRole
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}

public sealed class RoleAccess
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public Guid PermissionId { get; set; }
    public AccessRight Permission { get; set; } = null!;
}

public sealed class Site : Entity
{
    private Site() { }
    public Site(string code, string name) { Code = code.Trim().ToUpperInvariant(); Name = name.Trim(); }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
}

public sealed class ServiceCatalog : Entity
{
    private ServiceCatalog() { }
    public ServiceCatalog(string code, string name) { Code = code.Trim().ToUpperInvariant(); Name = name.Trim(); }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
}

public sealed class ScreenDefinition : Entity
{
    private ScreenDefinition() { }
    public ScreenDefinition(string code, string name, string route) { Code = code; Name = name; Route = route; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Route { get; private set; } = string.Empty;
}

public sealed class MenuItem : Entity
{
    private MenuItem() { }
    public MenuItem(string code, string label, string route, int order, string requiredPermission)
    { Code = code; Label = label; Route = route; Order = order; RequiredPermission = requiredPermission; }
    public string Code { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public string Route { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public string RequiredPermission { get; private set; } = string.Empty;
}

public sealed class SystemParameter : Entity
{
    private SystemParameter() { }
    public SystemParameter(string group, string key, string value, string description)
    { Group = group; Key = key; Value = value; Description = description; }
    public string Group { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public void Update(string value) { Value = value; Touch(); }
}

public enum BookingStatus { Draft, PendingConfirmation, Confirmed, InProgress, ReplanningRequested, Completed, Cancelled }
public enum AssignmentRole { Responsible, Collaborator }
public enum ConfirmationStatus { Pending, Confirmed, Rejected }

public sealed class Booking : Entity
{
    private Booking() { }
    public Booking(Guid requirementId, Guid activityId, Guid serviceId, Guid siteId, string title, string priority, decimal estimatedHours)
    {
        if (requirementId == Guid.Empty || activityId == Guid.Empty || serviceId == Guid.Empty || siteId == Guid.Empty)
            throw new ArgumentException("Requirement, activity, service and site are required.");
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(estimatedHours);
        RequirementId = requirementId; ActivityId = activityId; ServiceId = serviceId; SiteId = siteId;
        Title = string.IsNullOrWhiteSpace(title) ? throw new ArgumentException("Title is required.") : title.Trim();
        Priority = string.IsNullOrWhiteSpace(priority) ? "Normal" : priority.Trim(); EstimatedHours = estimatedHours;
    }
    public Guid RequirementId { get; private set; }
    public Guid ActivityId { get; private set; }
    public Guid ServiceId { get; private set; }
    public Guid SiteId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Priority { get; private set; } = "Normal";
    public decimal EstimatedHours { get; private set; }
    public BookingStatus Status { get; private set; } = BookingStatus.Draft;
    public int Version { get; private set; } = 1;
    public ICollection<BookingAssignment> Assignments { get; } = [];
    public void Submit()
    {
        if (!Assignments.Any(x => x.Role == AssignmentRole.Responsible)) throw new InvalidOperationException("A responsible assignment is required.");
        if (!Assignments.SelectMany(x => x.Blocks).Any()) throw new InvalidOperationException("At least one time block is required.");
        Status = BookingStatus.PendingConfirmation; Version++; Touch();
    }
}

public sealed class BookingAssignment : Entity
{
    private BookingAssignment() { }
    public BookingAssignment(Guid userId, AssignmentRole role, decimal allocatedHours)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User is required.");
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(allocatedHours);
        UserId = userId; Role = role; AllocatedHours = allocatedHours;
    }
    public Guid BookingId { get; private set; }
    public Booking Booking { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public AssignmentRole Role { get; private set; }
    public decimal AllocatedHours { get; private set; }
    public ConfirmationStatus Confirmation { get; private set; } = ConfirmationStatus.Pending;
    public string? ConfirmationComment { get; private set; }
    public ICollection<TimeBlock> Blocks { get; } = [];
    public void AddBlock(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start) throw new ArgumentException("End must be after start.");
        Blocks.Add(new TimeBlock(start, end)); Touch();
    }
    public void Confirm(bool accepted, string? comment)
    { Confirmation = accepted ? ConfirmationStatus.Confirmed : ConfirmationStatus.Rejected; ConfirmationComment = comment?.Trim(); Touch(); }
}

public sealed class TimeBlock : Entity
{
    private TimeBlock() { }
    public TimeBlock(DateTimeOffset start, DateTimeOffset end) { Start = start; End = end; }
    public Guid AssignmentId { get; private set; }
    public BookingAssignment Assignment { get; private set; } = null!;
    public DateTimeOffset Start { get; private set; }
    public DateTimeOffset End { get; private set; }
    public decimal Hours => (decimal)(End - Start).TotalHours;
}

public sealed class AuditEntry
{
    public long Id { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? ActorId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string DataJson { get; set; } = "{}";
}

public sealed class UsageMetric
{
    public long Id { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? UserId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string ScreenCode { get; set; } = string.Empty;
    public int DurationMs { get; set; }
    public string MetadataJson { get; set; } = "{}";
}
