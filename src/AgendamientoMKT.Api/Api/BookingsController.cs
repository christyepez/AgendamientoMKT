using AgendamientoMKT.Api.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendamientoMKT.Api.Api;

[ApiController, Route("api/bookings"), Authorize(Policy = "BOOKING.VIEW")]
public sealed class BookingsController(BookingService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<BookingDto>> List(CancellationToken ct) => service.ListAsync(ct);

    [HttpGet("{id:guid}")]
    public Task<BookingDto> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);

    [HttpPost, Authorize(Policy = "BOOKING.MANAGE")]
    public async Task<ActionResult> Create(CreateBookingRequest request, CancellationToken ct)
    { var id = await service.CreateAsync(request, ct); return CreatedAtAction(nameof(Get), new { id }, new { id }); }

    [HttpPost("{id:guid}/assignments"), Authorize(Policy = "BOOKING.MANAGE")]
    public async Task<IActionResult> AddAssignment(Guid id, AddAssignmentRequest request, CancellationToken ct)
    { await service.AddAssignmentAsync(id, request, ct); return NoContent(); }

    [HttpPost("{id:guid}/assignments/{assignmentId:guid}/blocks"), Authorize(Policy = "BOOKING.MANAGE")]
    public async Task<IActionResult> AddBlock(Guid id, Guid assignmentId, AddBlockRequest request, CancellationToken ct)
    { await service.AddBlockAsync(id, assignmentId, request, ct); return NoContent(); }

    [HttpPost("{id:guid}/submit"), Authorize(Policy = "BOOKING.MANAGE")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    { await service.SubmitAsync(id, ct); return NoContent(); }

    [HttpPost("{id:guid}/assignments/{assignmentId:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, Guid assignmentId, ConfirmAssignmentRequest request, CancellationToken ct)
    { await service.ConfirmAsync(id, assignmentId, request, ct); return NoContent(); }
}
