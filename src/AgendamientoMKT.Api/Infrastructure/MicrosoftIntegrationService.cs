using System.Net.Http.Headers;
using System.Text.Json;
using AgendamientoMKT.Api.Application;

namespace AgendamientoMKT.Api.Infrastructure;

public sealed class Microsoft365Options
{
    public const string Section = "MicrosoftGraph";
    public string TenantId { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
}

public sealed class MicrosoftIntegrationService(IHttpClientFactory clients, IConfiguration configuration, IClock clock)
{
    private static readonly (string Code, string Name)[] Products =
    [
        ("GRAPH", "Microsoft Graph"), ("OUTLOOK", "Outlook"), ("TEAMS", "Teams"),
        ("PLANNER", "Planner"), ("POWER_BI", "Power BI"), ("POWER_AUTOMATE", "Power Automate")
    ];

    public async Task<IReadOnlyCollection<IntegrationStatusDto>> StatusAsync(bool testConnection, CancellationToken ct)
    {
        var options = configuration.GetSection(Microsoft365Options.Section).Get<Microsoft365Options>() ?? new();
        var configured = !string.IsNullOrWhiteSpace(options.TenantId) && !string.IsNullOrWhiteSpace(options.ClientId) && !string.IsNullOrWhiteSpace(options.ClientSecret);
        var available = false;
        var status = configured ? "Configurado" : "Pendiente de credenciales";
        if (configured && testConnection)
        {
            try { await AcquireTokenAsync(options, ct); available = true; status = "Conexión verificada"; }
            catch (HttpRequestException) { status = "Sin conexión"; }
            catch (InvalidOperationException) { status = "Credenciales rechazadas"; }
        }
        return Products.Select(x => new IntegrationStatusDto(x.Code, x.Name, configured, available, status, clock.UtcNow)).ToArray();
    }

    private async Task<string> AcquireTokenAsync(Microsoft365Options options, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://login.microsoftonline.com/{Uri.EscapeDataString(options.TenantId)}/oauth2/v2.0/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = options.ClientId, ["client_secret"] = options.ClientSecret,
                ["scope"] = "https://graph.microsoft.com/.default", ["grant_type"] = "client_credentials"
            })
        };
        using var response = await clients.CreateClient("MicrosoftGraph").SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException("Microsoft identity rejected the application credentials.");
        using var document = JsonDocument.Parse(json);
        return document.RootElement.TryGetProperty("access_token", out var token) ? token.GetString() ?? throw new InvalidOperationException("Token response is empty.") : throw new InvalidOperationException("Token response is invalid.");
    }
}
