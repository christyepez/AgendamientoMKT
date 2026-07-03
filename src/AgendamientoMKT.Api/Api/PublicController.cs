using AgendamientoMKT.Api.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendamientoMKT.Api.Api;

[ApiController, Route("api/public"), AllowAnonymous]
public sealed class PublicController : ControllerBase
{
    [HttpGet("availability")]
    public IActionResult Availability([FromQuery] Guid siteId, [FromQuery] Guid serviceId, [FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        if (siteId == Guid.Empty || serviceId == Guid.Empty || to < from || to.DayNumber - from.DayNumber > 62) return BadRequest(new { message = "Invalid availability range." });
        var days = Enumerable.Range(0, to.DayNumber - from.DayNumber + 1).Select(i => from.AddDays(i)).Where(x => x.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday).Select(x => new { date = x, availability = "Available" });
        return Ok(new { siteId, serviceId, days });
    }
}
