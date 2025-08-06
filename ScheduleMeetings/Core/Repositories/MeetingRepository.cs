using ScheduleMeetings.Core.Interfaces;
using ScheduleMeetings.Data;
using ScheduleMeetings.Entities;

namespace ScheduleMeetings.Core.Services;

public class MeetingRepository : IMeetingRepository
{
    private List<Meeting> meetings;
    public MeetingRepository(IUserRepository userService)
    {
        meetings = new List<Meeting>();
        TimeSlot timeSlot1 = new TimeSlot(new DateTime(2025, 08, 05, 10, 0, 0, DateTimeKind.Utc),
        new DateTime(2025, 08, 05, 11, 0, 0, DateTimeKind.Utc));
        TimeSlot timeSlot2 = new TimeSlot(new DateTime(2025, 08, 05, 13, 15, 0, DateTimeKind.Utc),
            new DateTime(2025, 08, 05, 13, 30, 0, DateTimeKind.Utc));
        TimeSlot timeSlot3 = new TimeSlot(new DateTime(2025, 08, 05, 13, 30, 0, DateTimeKind.Utc),
            new DateTime(2025, 08, 05, 13, 45, 0, DateTimeKind.Utc));

        TimeSlot timeSlot4 = new TimeSlot(new DateTime(2025, 08, 05, 14, 30, 0, DateTimeKind.Utc),
            new DateTime(2025, 08, 05, 16, 10, 0, DateTimeKind.Utc));

        meetings.Add(new Meeting(1, [userService.Users[0]], timeSlot1));
        meetings.Add(new Meeting(2, [userService.Users[0]], timeSlot2));
        meetings.Add(new Meeting(3, [userService.Users[1]], timeSlot3));
        meetings.Add(new Meeting(3, [userService.Users[1]], timeSlot4));
    }
    public Meeting Add(Meeting meeting)
    {
        meetings.Add(meeting);
        return meeting;
    }
    public IEnumerable<Meeting> GetAllMeetingsByUser(int id)
    {
        return meetings.Where(m => m.users.Any(u => u.Id == id));
    }
    public List<Meeting> Meetings
    {
        get => meetings;
    }
}
