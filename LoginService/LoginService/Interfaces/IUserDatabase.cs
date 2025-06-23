using LoginService.Models;

namespace LoginService.Interfaces
{
    public interface IUserDatabase
    {

        public void AddUser(User user);

        public void RemoveUser(User user);

        public User? GetUserByUsername(string username);

        public User? GetUserByEmail(string email);

        public User? FindUser(UserDto user);

    }
}
