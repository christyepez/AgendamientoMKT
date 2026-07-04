using AgendamientoMKT.Api.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendamientoMKT.Api.Api;

[ApiController, Route("api/administration"), Authorize]
public sealed class AdministrationController(IAdministrationRepository repository, AdministrationService service) : ControllerBase
{
    [HttpGet("lookups")]
    public async Task<object> Lookups(CancellationToken ct) => new { roles = await repository.RolesAsync(ct), sites = await repository.SitesAsync(ct), services = await repository.ServicesAsync(ct) };

    [HttpGet("menu")]
    public Task<IReadOnlyCollection<MenuDto>> Menu(CancellationToken ct) => repository.MenuAsync(User.FindAll("permission").Select(x => x.Value).ToArray(), ct);

    [HttpGet("users"), Authorize(Policy = "USERS.MANAGE")]
    public Task<IReadOnlyCollection<UserListDto>> Users(CancellationToken ct) => service.UsersAsync(ct);

    [HttpPost("users"), Authorize(Policy = "USERS.MANAGE")]
    public async Task<ActionResult> CreateUser(CreateUserRequest request, CancellationToken ct)
    {
        var id = await service.CreateUserAsync(request, ct); return Created($"api/administration/users/{id}", new { id });
    }

    [HttpGet("parameters"), Authorize(Policy = "PARAMETERS.MANAGE")]
    public Task<IReadOnlyCollection<ParameterDto>> Parameters(CancellationToken ct) => service.ParametersAsync(ct);

    [HttpGet("configuration-center"), Authorize(Policy = "PARAMETERS.MANAGE")]
    public Task<ConfigurationCenterDto> ConfigurationCenter(CancellationToken ct) => service.ConfigurationCenterAsync(ct);

    [HttpPut("parameters/{id:guid}"), Authorize(Policy = "PARAMETERS.MANAGE")]
    public async Task<IActionResult> UpdateParameter(Guid id, UpdateParameterRequest request, CancellationToken ct)
    {
        await service.UpdateParameterAsync(id, request, ct); return NoContent();
    }

    [HttpGet("audit"), Authorize(Policy = "AUDIT.VIEW")]
    public Task<IReadOnlyCollection<AuditDto>> Audit([FromQuery] int take, CancellationToken ct) => service.AuditAsync(take, ct);

    [HttpGet("metrics"), Authorize(Policy = "METRICS.VIEW")]
    public Task<IReadOnlyCollection<MetricSummaryDto>> Metrics([FromQuery] int days, CancellationToken ct) => service.MetricsAsync(days, ct);
}
