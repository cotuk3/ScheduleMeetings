using ScheduleMeetings.Data;

namespace ScheduleMeetings.Core.Interfaces;

public interface IMeetingRepository
{
    Meeting Add(Meeting meeting);
    IEnumerable<Meeting> GetAllMeetingsByUser(int id);
    List<Meeting> Meetings { get; }
}
