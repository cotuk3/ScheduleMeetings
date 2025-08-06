using ScheduleMeetings.Dtos;
using ScheduleMeetings.Entities;

namespace ScheduleMeetings.Core.Interfaces;

public interface IMeetingService
{
    TimeSlot FreeTimeSlotForMeeting(MeetingFindDto dto);

}
