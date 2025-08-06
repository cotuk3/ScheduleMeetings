using ScheduleMeetings.Data;

namespace ScheduleMeetings.Core.Interfaces;

public interface IUserRepository
{
    User Add(User user);
    List<User> Users { get; }
}
