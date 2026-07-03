using System.Security.Claims;
using System.Text.Json;
using AgendamientoMKT.Api.Application;
using AgendamientoMKT.Api.Infrastructure.Persistence;

namespace AgendamientoMKT.Api.Api;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId => Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? accessor.HttpContext?.User.FindFirstValue("sub"), out var id) ? id : null;
}

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private static readonly Action<ILogger, Exception?> LogUnhandled = LoggerMessage.Define(LogLevel.Error, new EventId(5000, "UnhandledRequest"), "Unhandled request error");
    private static readonly Action<ILogger, string, Exception?> LogRejected = LoggerMessage.Define<string>(LogLevel.Warning, new EventId(4000, "RejectedRequest"), "Request rejected: {Message}");
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception ex)
        {
            var status = ex switch { KeyNotFoundException => 404, ArgumentException => 400, InvalidOperationException => 409, _ => 500 };
            if (status == 500) LogUnhandled(logger, ex); else LogRejected(logger, ex.Message, ex);
            context.Response.StatusCode = status; context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { type = "about:blank", title = status == 500 ? "Unexpected error" : ex.Message, status, traceId = context.TraceIdentifier }));
        }
    }
}

public sealed class UsageTrackingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AppDbContext db, ICurrentUser currentUser)
    {
        var started = System.Diagnostics.Stopwatch.StartNew(); await next(context); started.Stop();
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            db.UsageMetrics.Add(new Domain.UsageMetric { UserId = currentUser.UserId, EventName = $"HTTP_{context.Request.Method}", ScreenCode = context.Request.Path.Value ?? string.Empty, DurationMs = (int)Math.Min(int.MaxValue, started.ElapsedMilliseconds), MetadataJson = JsonSerializer.Serialize(new { context.Response.StatusCode }) });
            await db.SaveChangesAsync(context.RequestAborted);
        }
    }
}
