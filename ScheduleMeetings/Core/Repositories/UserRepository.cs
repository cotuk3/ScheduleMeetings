using ScheduleMeetings.Core.Interfaces;
using ScheduleMeetings.Data;

namespace ScheduleMeetings.Core.Services;

public class UserRepository : IUserRepository
{
    private List<User> users;
    public UserRepository()
    {
        users = [
            new User(1, "Alice"),
            new User(2, "Julio")];
    }
    public User Add(User user)
    {
        users.Add(user);
        return user;
    }

    public List<User> Users
    {
        get => users;
    }
}
