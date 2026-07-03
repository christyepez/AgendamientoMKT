using System.Text.Json;
using AgendamientoMKT.Api.Application;
using AgendamientoMKT.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AgendamientoMKT.Api.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<AccessRight> Permissions => Set<AccessRight>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<ServiceCatalog> Services => Set<ServiceCatalog>();
    public DbSet<ScreenDefinition> Screens => Set<ScreenDefinition>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<SystemParameter> Parameters => Set<SystemParameter>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingAssignment> BookingAssignments => Set<BookingAssignment>();
    public DbSet<TimeBlock> TimeBlocks => Set<TimeBlock>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<UsageMetric> UsageMetrics => Set<UsageMetric>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("booking");
        modelBuilder.Entity<AppUser>(e => { e.ToTable("Users", "identity"); e.HasIndex(x => x.Email).IsUnique(); e.Property(x => x.Email).HasMaxLength(256); e.Property(x => x.DisplayName).HasMaxLength(200); e.Property(x => x.PasswordHash).HasMaxLength(1000); });
        modelBuilder.Entity<Role>(e => { e.ToTable("Roles", "identity"); e.HasIndex(x => x.Code).IsUnique(); e.Property(x => x.Code).HasMaxLength(80); });
        modelBuilder.Entity<AccessRight>(e => { e.ToTable("Permissions", "identity"); e.HasIndex(x => x.Code).IsUnique(); e.Property(x => x.Code).HasMaxLength(120); });
        modelBuilder.Entity<UserRole>(e => { e.ToTable("UserRoles", "identity"); e.HasKey(x => new { x.UserId, x.RoleId }); e.HasOne(x => x.User).WithMany(x => x.Roles).HasForeignKey(x => x.UserId); e.HasOne(x => x.Role).WithMany(x => x.Users).HasForeignKey(x => x.RoleId); });
        modelBuilder.Entity<RoleAccess>(e => { e.ToTable("RolePermissions", "identity"); e.HasKey(x => new { x.RoleId, x.PermissionId }); e.HasOne(x => x.Role).WithMany(x => x.Permissions).HasForeignKey(x => x.RoleId); });
        modelBuilder.Entity<Site>(e => { e.ToTable("Sites", "catalog"); e.HasIndex(x => x.Code).IsUnique(); });
        modelBuilder.Entity<ServiceCatalog>(e => { e.ToTable("Services", "catalog"); e.HasIndex(x => x.Code).IsUnique(); });
        modelBuilder.Entity<ScreenDefinition>(e => { e.ToTable("Screens", "catalog"); e.HasIndex(x => x.Code).IsUnique(); });
        modelBuilder.Entity<MenuItem>(e => { e.ToTable("MenuItems", "catalog"); e.HasIndex(x => x.Code).IsUnique(); });
        modelBuilder.Entity<SystemParameter>(e => { e.ToTable("Parameters", "catalog"); e.HasIndex(x => new { x.Group, x.Key }).IsUnique(); });
        modelBuilder.Entity<Booking>(e => { e.ToTable("Bookings", "booking", b => b.IsTemporal()); e.Property(x => x.Status).HasConversion<string>().HasMaxLength(40); e.Property(x => x.EstimatedHours).HasPrecision(9, 2); e.HasIndex(x => x.ActivityId).IsUnique(); });
        modelBuilder.Entity<BookingAssignment>(e => { e.ToTable("Assignments", "booking", b => b.IsTemporal()); e.Property(x => x.Role).HasConversion<string>(); e.Property(x => x.Confirmation).HasConversion<string>(); e.Property(x => x.AllocatedHours).HasPrecision(9, 2); e.HasOne(x => x.Booking).WithMany(x => x.Assignments).HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.BookingId, x.UserId }).IsUnique(); });
        modelBuilder.Entity<TimeBlock>(e => { e.ToTable("TimeBlocks", "booking", b => b.IsTemporal()); e.Ignore(x => x.Hours); e.HasOne(x => x.Assignment).WithMany(x => x.Blocks).HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.AssignmentId, x.Start, x.End }); });
        modelBuilder.Entity<AuditEntry>(e => { e.ToTable("AuditEntries", "audit"); e.HasKey(x => x.Id); e.Property(x => x.Action).HasMaxLength(100); e.Property(x => x.EntityType).HasMaxLength(100); e.HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAt }); });
        modelBuilder.Entity<UsageMetric>(e => { e.ToTable("UsageMetrics", "audit"); e.HasKey(x => x.Id); e.Property(x => x.EventName).HasMaxLength(100); e.Property(x => x.ScreenCode).HasMaxLength(100); e.HasIndex(x => new { x.ScreenCode, x.OccurredAt }); });
    }
}

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<AppUser?> FindByEmailAsync(string email, CancellationToken ct)
    { var normalizedEmail = email.Trim().ToLowerInvariant(); return await db.Users.Include(x => x.Roles).ThenInclude(x => x.Role).ThenInclude(x => x.Permissions).ThenInclude(x => x.Permission).SingleOrDefaultAsync(x => x.Email == normalizedEmail, ct); }
    public async Task<AppUser?> GetAsync(Guid id, CancellationToken ct) => await db.Users.Include(x => x.Roles).ThenInclude(x => x.Role).SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyCollection<AppUser>> ListAsync(CancellationToken ct) => await db.Users.AsNoTracking().OrderBy(x => x.DisplayName).ToArrayAsync(ct);
    public async Task AddAsync(AppUser user, CancellationToken ct) => await db.Users.AddAsync(user, ct);
}

public sealed class BookingRepository(AppDbContext db) : IBookingRepository
{
    public async Task<Booking?> GetAsync(Guid id, CancellationToken ct) => await db.Bookings.Include(x => x.Assignments).ThenInclude(x => x.Blocks).SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyCollection<Booking>> ListAsync(CancellationToken ct) => await db.Bookings.AsNoTracking().Include(x => x.Assignments).ThenInclude(x => x.Blocks).OrderByDescending(x => x.CreatedAt).ToArrayAsync(ct);
    public async Task AddAsync(Booking booking, CancellationToken ct) => await db.Bookings.AddAsync(booking, ct);
    public Task<bool> HasConflictAsync(Guid userId, DateTimeOffset start, DateTimeOffset endAt, Guid? excludedBookingId, CancellationToken ct) =>
        db.TimeBlocks.AnyAsync(x => x.Assignment.UserId == userId && x.Assignment.BookingId != excludedBookingId && x.Start < endAt && start < x.End && x.Assignment.Booking.Status != BookingStatus.Cancelled, ct);
}

public sealed class AdministrationRepository(AppDbContext db) : IAdministrationRepository
{
    public async Task<IReadOnlyCollection<LookupDto>> RolesAsync(CancellationToken ct) => await db.Roles.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new LookupDto(x.Id, x.Code, x.Name)).ToArrayAsync(ct);
    public async Task<IReadOnlyDictionary<Guid, Role>> RolesByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) => await db.Roles.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
    public async Task<IReadOnlyCollection<LookupDto>> SitesAsync(CancellationToken ct) => await db.Sites.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new LookupDto(x.Id, x.Code, x.Name)).ToArrayAsync(ct);
    public async Task<IReadOnlyCollection<LookupDto>> ServicesAsync(CancellationToken ct) => await db.Services.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new LookupDto(x.Id, x.Code, x.Name)).ToArrayAsync(ct);
    public async Task<IReadOnlyCollection<MenuDto>> MenuAsync(IReadOnlyCollection<string> permissions, CancellationToken ct) => await db.MenuItems.AsNoTracking().Where(x => x.IsActive && permissions.Contains(x.RequiredPermission)).OrderBy(x => x.Order).Select(x => new MenuDto(x.Code, x.Label, x.Route, x.Order, x.RequiredPermission)).ToArrayAsync(ct);
    public async Task<IReadOnlyCollection<ParameterDto>> ParametersAsync(CancellationToken ct) => await db.Parameters.AsNoTracking().OrderBy(x => x.Group).ThenBy(x => x.Key).Select(x => new ParameterDto(x.Id, x.Group, x.Key, x.Value, x.Description)).ToArrayAsync(ct);
    public Task<SystemParameter?> ParameterAsync(Guid id, CancellationToken ct) => db.Parameters.SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyCollection<AuditDto>> AuditAsync(int take, CancellationToken ct) => await db.AuditEntries.AsNoTracking().OrderByDescending(x => x.OccurredAt).Take(Math.Clamp(take, 1, 500)).Select(x => new AuditDto(x.Id, x.OccurredAt, x.ActorId, x.Action, x.EntityType, x.EntityId, x.DataJson)).ToArrayAsync(ct);
    public async Task<IReadOnlyCollection<MetricSummaryDto>> MetricsAsync(DateTimeOffset from, CancellationToken ct) => await db.UsageMetrics.AsNoTracking().Where(x => x.OccurredAt >= from).GroupBy(x => x.ScreenCode).Select(x => new MetricSummaryDto(x.Key, x.Count(), x.Average(v => v.DurationMs), x.Count(v => v.MetadataJson.Contains("500")))).OrderByDescending(x => x.Visits).ToArrayAsync(ct);
    public async Task<IReadOnlyCollection<UserListDto>> UsersAsync(CancellationToken ct) => await db.Users.AsNoTracking().Include(x => x.Roles).ThenInclude(x => x.Role).OrderBy(x => x.DisplayName).Select(x => new UserListDto(x.Id, x.Email, x.DisplayName, x.SiteId, x.IsActive, x.Roles.Select(r => r.Role.Code).ToArray())).ToArrayAsync(ct);
}

public sealed class AuditWriter(AppDbContext db, ICurrentUser currentUser) : IAuditWriter
{
    public async Task WriteAsync(string action, string entityType, string entityId, object data, CancellationToken ct)
    {
        db.AuditEntries.Add(new AuditEntry { ActorId = currentUser.UserId, Action = action, EntityType = entityType, EntityId = entityId, DataJson = JsonSerializer.Serialize(data) });
        await db.SaveChangesAsync(ct);
    }
}

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(AppDbContext db, IPasswordHasher<AppUser> hasher, IConfiguration configuration, CancellationToken ct)
    {
        await db.Database.EnsureCreatedAsync(ct);
        if (await db.Roles.AnyAsync(ct)) return;

        var sites = new[] { new Site("AMB", "Ambato"), new Site("UIO", "Quito"), new Site("ONL", "Online") };
        var services = new[] { new ServiceCatalog("DESIGN", "Diseño"), new ServiceCatalog("VIDEO", "Video"), new ServiceCatalog("PHOTO", "Fotografía"), new ServiceCatalog("SOCIAL", "Redes"), new ServiceCatalog("EVENT", "Eventos") };
        var permissionCodes = new[] { "DASHBOARD.VIEW", "BOOKING.VIEW", "BOOKING.MANAGE", "BOOKING.APPROVE", "AGENDA.VIEW", "USERS.MANAGE", "ROLES.MANAGE", "PARAMETERS.MANAGE", "AUDIT.VIEW", "METRICS.VIEW" };
        var permissions = permissionCodes.ToDictionary(x => x, x => new AccessRight(x, x));
        var admin = new Role("ADMIN", "Administrador"); var coordinator = new Role("COORDINATOR", "Coordinador"); var member = new Role("MEMBER", "Integrante"); var approver = new Role("APPROVER", "Aprobador");
        foreach (var permission in permissions.Values) admin.Permissions.Add(new RoleAccess { Role = admin, Permission = permission, PermissionId = permission.Id, RoleId = admin.Id });
        foreach (var code in new[] { "DASHBOARD.VIEW", "BOOKING.VIEW", "BOOKING.MANAGE", "AGENDA.VIEW", "AUDIT.VIEW", "METRICS.VIEW" }) coordinator.Permissions.Add(new RoleAccess { Role = coordinator, Permission = permissions[code], PermissionId = permissions[code].Id, RoleId = coordinator.Id });
        foreach (var code in new[] { "DASHBOARD.VIEW", "BOOKING.VIEW", "AGENDA.VIEW" }) member.Permissions.Add(new RoleAccess { Role = member, Permission = permissions[code], PermissionId = permissions[code].Id, RoleId = member.Id });
        foreach (var code in new[] { "DASHBOARD.VIEW", "BOOKING.VIEW", "BOOKING.APPROVE", "AUDIT.VIEW" }) approver.Permissions.Add(new RoleAccess { Role = approver, Permission = permissions[code], PermissionId = permissions[code].Id, RoleId = approver.Id });
        var roles = new[] { admin, coordinator, member, approver, new Role("REQUESTER", "Solicitante"), new Role("PROVIDER", "Proveedor") };
        var screens = new[] { new ScreenDefinition("DASHBOARD", "Dashboard", "/dashboard"), new ScreenDefinition("BOOKINGS", "Booking", "/bookings"), new ScreenDefinition("AGENDA", "Mi agenda", "/my-agenda"), new ScreenDefinition("USERS", "Usuarios", "/admin/users"), new ScreenDefinition("PARAMETERS", "Parametrizaciones", "/admin/parameters"), new ScreenDefinition("AUDIT", "Auditoría", "/admin/audit"), new ScreenDefinition("METRICS", "Métricas", "/admin/metrics") };
        var menu = screens.Select((x, i) => new MenuItem(x.Code, x.Name, x.Route, i + 1, x.Code switch { "USERS" => "USERS.MANAGE", "PARAMETERS" => "PARAMETERS.MANAGE", "AUDIT" => "AUDIT.VIEW", "METRICS" => "METRICS.VIEW", "AGENDA" => "AGENDA.VIEW", "BOOKINGS" => "BOOKING.VIEW", _ => "DASHBOARD.VIEW" })).ToArray();
        var parameters = new[] { new SystemParameter("WORK_SCHEDULE", "START", "08:30", "Inicio de jornada"), new SystemParameter("WORK_SCHEDULE", "END", "17:30", "Fin de jornada"), new SystemParameter("WORK_SCHEDULE", "DAILY_HOURS", "8", "Horas diarias"), new SystemParameter("REPLANNING", "MIN_DAYS", "14", "Anticipación mínima"), new SystemParameter("PUBLIC", "AVAILABILITY_DETAIL", "AGGREGATED", "Visibilidad externa") };
        db.AddRange(sites); db.AddRange(services); db.AddRange(permissions.Values); db.AddRange(roles); db.AddRange(screens); db.AddRange(menu); db.AddRange(parameters);

        var email = configuration["Seed:AdminEmail"] ?? "admin@agendamientomkt.local";
        var password = configuration["Seed:AdminPassword"] ?? "Admin.ChangeMe.2026!";
        var user = new AppUser(email, "Administrador", sites[0].Id); user.SetPasswordHash(hasher.HashPassword(user, password));
        user.Roles.Add(new UserRole { User = user, UserId = user.Id, Role = admin, RoleId = admin.Id }); db.Users.Add(user);
        await db.SaveChangesAsync(ct);
    }
}
