using AgendamientoMKT.Api.Application;
using AgendamientoMKT.Api.Domain;
using Xunit;

namespace AgendamientoMKT.UnitTests;

public sealed class BookingDomainTests
{
    [Fact]
    public void Submit_WithoutResponsible_RejectsTransition()
    {
        var booking = CreateBooking();
        var collaborator = new BookingAssignment(Guid.NewGuid(), AssignmentRole.Collaborator, 2);
        collaborator.AddBlock(At(9), At(11));
        booking.Assignments.Add(collaborator);

        var exception = Assert.Throws<InvalidOperationException>(booking.Submit);

        Assert.Equal("A responsible assignment is required.", exception.Message);
        Assert.Equal(BookingStatus.Draft, booking.Status);
    }

    [Fact]
    public void Submit_WithResponsibleAndBlock_MovesToPendingConfirmation()
    {
        var booking = CreateBooking();
        var responsible = new BookingAssignment(Guid.NewGuid(), AssignmentRole.Responsible, 2);
        responsible.AddBlock(At(9), At(11));
        booking.Assignments.Add(responsible);

        booking.Submit();

        Assert.Equal(BookingStatus.PendingConfirmation, booking.Status);
        Assert.Equal(2, booking.Version);
    }

    [Theory]
    [InlineData(7, 30, 9, 0)]
    [InlineData(16, 30, 18, 0)]
    public void ValidateWorkSchedule_OutsideWorkingHours_IsRejected(int startHour, int startMinute, int endHour, int endMinute)
    {
        var start = At(startHour, startMinute);
        var end = At(endHour, endMinute);

        Assert.Throws<InvalidOperationException>(() => BookingService.ValidateWorkSchedule(start, end));
    }

    [Fact]
    public void AddBlock_WithInvalidRange_IsRejected()
    {
        var assignment = new BookingAssignment(Guid.NewGuid(), AssignmentRole.Responsible, 2);

        Assert.Throws<ArgumentException>(() => assignment.AddBlock(At(11), At(10)));
    }

    private static Booking CreateBooking() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Campaña institucional", "High", 8);
    private static DateTimeOffset At(int hour, int minute = 0) => new(2026, 7, 6, hour, minute, 0, TimeSpan.FromHours(-5));
}
