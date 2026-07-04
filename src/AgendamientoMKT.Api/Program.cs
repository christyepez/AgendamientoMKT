using System.Text;
using AgendamientoMKT.Api.Api;
using AgendamientoMKT.Api.Application;
using AgendamientoMKT.Api.Domain;
using AgendamientoMKT.Api.Infrastructure;
using AgendamientoMKT.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEncryptedSecrets(builder.Environment.ContentRootPath);
var connectionString = builder.Configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("ConnectionStrings:Default is required.");
var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? throw new InvalidOperationException("JWT configuration is required.");
if (Encoding.UTF8.GetByteCount(jwt.Key) < 32) throw new InvalidOperationException("Jwt:Key must contain at least 32 bytes.");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Section));
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(5)));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IAdministrationRepository, AdministrationRepository>();
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<IAuditWriter, AuditWriter>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<AdministrationService>();
builder.Services.Configure<Microsoft365Options>(builder.Configuration.GetSection(Microsoft365Options.Section));
builder.Services.AddHttpClient("MicrosoftGraph", client => { client.Timeout = TimeSpan.FromSeconds(15); client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); });
builder.Services.AddScoped<MicrosoftIntegrationService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = jwt.Issuer,
        ValidateAudience = true, ValidAudience = jwt.Audience,
        ValidateLifetime = true, ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddAuthorization(options =>
{
    ReadOnlySpan<string> permissionPolicies = ["DASHBOARD.VIEW", "BOOKING.VIEW", "BOOKING.MANAGE", "BOOKING.APPROVE", "AGENDA.VIEW", "USERS.MANAGE", "ROLES.MANAGE", "PARAMETERS.MANAGE", "AUDIT.VIEW", "METRICS.VIEW"];
    foreach (var permission in permissionPolicies)
        options.AddPolicy(permission, policy => policy.RequireClaim("permission", permission));
});
var defaultOrigins = new[] { "http://localhost:3001" };
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? defaultOrigins).AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseMiddleware<ExceptionMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UsageTrackingMiddleware>();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

if (app.Configuration.GetValue("Database:Initialize", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    await DatabaseInitializer.InitializeAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(), scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>(), app.Configuration, CancellationToken.None);
}

await app.RunAsync();

public partial class Program;
