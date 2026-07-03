using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AgendamientoMKT.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AgendamientoMKT.Api.Application;

public sealed class JwtOptions
{
    public const string Section = "Jwt";
    public string Issuer { get; init; } = "AgendamientoMKT";
    public string Audience { get; init; } = "AgendamientoMKT";
    public string Key { get; init; } = string.Empty;
    public int ExpirationMinutes { get; init; } = 480;
}

public sealed class AuthService(IUserRepository users, IPasswordHasher<AppUser> hasher, IOptions<JwtOptions> options, IClock clock)
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email, ct);
        if (user is null || !user.IsActive || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed) return null;
        var roles = user.Roles.Select(x => x.Role.Code).Distinct().ToArray();
        var permissions = user.Roles.SelectMany(x => x.Role.Permissions).Select(x => x.Permission.Code).Distinct().ToArray();
        var now = clock.UtcNow; var expires = now.AddMinutes(options.Value.ExpirationMinutes);
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new(JwtRegisteredClaimNames.Email, user.Email), new("name", user.DisplayName) };
        claims.AddRange(roles.Select(x => new Claim(ClaimTypes.Role, x)));
        claims.AddRange(permissions.Select(x => new Claim("permission", x)));
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.Key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(options.Value.Issuer, options.Value.Audience, claims, now.UtcDateTime, expires.UtcDateTime, credentials);
        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, new UserProfile(user.Id, user.Email, user.DisplayName, user.SiteId, roles, permissions));
    }
}

public sealed class UserService(IUserRepository users, IUnitOfWork unit, IPasswordHasher<AppUser> hasher, IAuditWriter audit)
{
    public async Task<Guid> CreateAsync(CreateUserRequest request, IReadOnlyDictionary<Guid, Role> roles, CancellationToken ct)
    {
        if (await users.FindByEmailAsync(request.Email, ct) is not null) throw new InvalidOperationException("Email is already registered.");
        var user = new AppUser(request.Email, request.DisplayName, request.SiteId);
        user.SetPasswordHash(hasher.HashPassword(user, request.Password));
        foreach (var roleId in request.RoleIds.Distinct())
            user.Roles.Add(new UserRole { User = user, UserId = user.Id, Role = roles.TryGetValue(roleId, out var role) ? role : throw new KeyNotFoundException("Role not found."), RoleId = roleId });
        await users.AddAsync(user, ct); await unit.SaveChangesAsync(ct);
        await audit.WriteAsync("USER_CREATED", nameof(AppUser), user.Id.ToString(), new { user.Email, user.DisplayName, user.SiteId }, ct);
        return user.Id;
    }
}

public sealed class AdministrationService(IAdministrationRepository repository, UserService users, IUnitOfWork unit, IAuditWriter audit, IClock clock)
{
    public Task<IReadOnlyCollection<UserListDto>> UsersAsync(CancellationToken ct) => repository.UsersAsync(ct);
    public async Task<Guid> CreateUserAsync(CreateUserRequest request, CancellationToken ct) => await users.CreateAsync(request, await repository.RolesByIdsAsync(request.RoleIds, ct), ct);
    public Task<IReadOnlyCollection<ParameterDto>> ParametersAsync(CancellationToken ct) => repository.ParametersAsync(ct);
    public async Task UpdateParameterAsync(Guid id, UpdateParameterRequest request, CancellationToken ct)
    {
        var parameter = await repository.ParameterAsync(id, ct) ?? throw new KeyNotFoundException("Parameter not found.");
        parameter.Update(request.Value); await unit.SaveChangesAsync(ct); await audit.WriteAsync("PARAMETER_UPDATED", nameof(SystemParameter), id.ToString(), request, ct);
    }
    public Task<IReadOnlyCollection<AuditDto>> AuditAsync(int take, CancellationToken ct) => repository.AuditAsync(take == 0 ? 100 : take, ct);
    public Task<IReadOnlyCollection<MetricSummaryDto>> MetricsAsync(int days, CancellationToken ct) => repository.MetricsAsync(clock.UtcNow.AddDays(-(days == 0 ? 30 : Math.Clamp(days, 1, 365))), ct);
}

public sealed class BookingService(IBookingRepository bookings, IUnitOfWork unit, IAuditWriter audit)
{
    public async Task<Guid> CreateAsync(CreateBookingRequest request, CancellationToken ct)
    {
        var booking = new Booking(request.RequirementId, request.ActivityId, request.ServiceId, request.SiteId, request.Title, request.Priority, request.EstimatedHours);
        await bookings.AddAsync(booking, ct); await unit.SaveChangesAsync(ct);
        await audit.WriteAsync("BOOKING_CREATED", nameof(Booking), booking.Id.ToString(), request, ct); return booking.Id;
    }

    public async Task AddAssignmentAsync(Guid bookingId, AddAssignmentRequest request, CancellationToken ct)
    {
        var booking = await RequiredAsync(bookingId, ct);
        if (booking.Status != BookingStatus.Draft) throw new InvalidOperationException("Assignments can only be changed in draft.");
        if (booking.Assignments.Any(x => x.UserId == request.UserId)) throw new InvalidOperationException("User is already assigned.");
        booking.Assignments.Add(new BookingAssignment(request.UserId, request.Role, request.AllocatedHours));
        await unit.SaveChangesAsync(ct); await audit.WriteAsync("ASSIGNMENT_ADDED", nameof(Booking), booking.Id.ToString(), request, ct);
    }

    public async Task AddBlockAsync(Guid bookingId, Guid assignmentId, AddBlockRequest request, CancellationToken ct)
    {
        ValidateWorkSchedule(request.Start, request.End);
        var booking = await RequiredAsync(bookingId, ct);
        if (booking.Status != BookingStatus.Draft) throw new InvalidOperationException("Blocks can only be changed in draft.");
        var assignment = booking.Assignments.SingleOrDefault(x => x.Id == assignmentId) ?? throw new KeyNotFoundException("Assignment not found.");
        if (await bookings.HasConflictAsync(assignment.UserId, request.Start, request.End, bookingId, ct)) throw new InvalidOperationException("The team member already has a booking in this period.");
        assignment.AddBlock(request.Start, request.End); await unit.SaveChangesAsync(ct);
        await audit.WriteAsync("TIME_BLOCK_ADDED", nameof(Booking), booking.Id.ToString(), request, ct);
    }

    public async Task SubmitAsync(Guid id, CancellationToken ct)
    { var booking = await RequiredAsync(id, ct); booking.Submit(); await unit.SaveChangesAsync(ct); await audit.WriteAsync("BOOKING_SUBMITTED", nameof(Booking), id.ToString(), new { booking.Version }, ct); }

    public async Task ConfirmAsync(Guid bookingId, Guid assignmentId, ConfirmAssignmentRequest request, CancellationToken ct)
    {
        var booking = await RequiredAsync(bookingId, ct);
        if (booking.Status != BookingStatus.PendingConfirmation) throw new InvalidOperationException("Booking is not awaiting confirmation.");
        var assignment = booking.Assignments.SingleOrDefault(x => x.Id == assignmentId) ?? throw new KeyNotFoundException("Assignment not found.");
        assignment.Confirm(request.Accepted, request.Comment); await unit.SaveChangesAsync(ct);
        await audit.WriteAsync("ASSIGNMENT_CONFIRMATION", nameof(Booking), bookingId.ToString(), request, ct);
    }

    public async Task<IReadOnlyCollection<BookingDto>> ListAsync(CancellationToken ct) => (await bookings.ListAsync(ct)).Select(Map).ToArray();
    public async Task<BookingDto> GetAsync(Guid id, CancellationToken ct) => Map(await RequiredAsync(id, ct));
    private async Task<Booking> RequiredAsync(Guid id, CancellationToken ct) => await bookings.GetAsync(id, ct) ?? throw new KeyNotFoundException("Booking not found.");
    private static BookingDto Map(Booking x) => new(x.Id, x.RequirementId, x.ActivityId, x.Title, x.Priority, x.EstimatedHours, x.Status.ToString(), x.Version,
        x.Assignments.Select(a => new AssignmentDto(a.Id, a.UserId, a.Role.ToString(), a.AllocatedHours, a.Confirmation.ToString(), a.Blocks.Select(b => new BlockDto(b.Id, b.Start, b.End, b.Hours)).ToArray())).ToArray());

    public static void ValidateWorkSchedule(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start) throw new ArgumentException("End must be after start.");
        if (start.Date != end.Date || start.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) throw new InvalidOperationException("Blocks must be on one business day.");
        var startTime = start.TimeOfDay; var endTime = end.TimeOfDay;
        if (startTime < new TimeSpan(8, 30, 0) || endTime > new TimeSpan(17, 30, 0)) throw new InvalidOperationException("Block is outside working hours.");
    }
}
