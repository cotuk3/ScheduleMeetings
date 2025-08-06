using ScheduleMeetings.Entities;

namespace ScheduleMeetings.Dtos;

public record class MeetingFindDto(
    List<int> Ids,
    int Duration,
    TimeSlot TimeSlot);
