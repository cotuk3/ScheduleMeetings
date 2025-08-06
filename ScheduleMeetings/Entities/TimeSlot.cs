namespace ScheduleMeetings.Entities;

public record class TimeSlot(
    DateTime StartTime,
    DateTime EndTime);

