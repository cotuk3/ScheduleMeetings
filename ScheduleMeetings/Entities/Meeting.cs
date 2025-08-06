using ScheduleMeetings.Entities;

namespace ScheduleMeetings.Data;

public record class Meeting(int Id, List<User> users, TimeSlot timeSlot);