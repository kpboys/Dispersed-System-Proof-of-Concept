using LoginService.Interfaces;
using LoginService.Models;

namespace LoginService
{
    public class UserDatabase : IUserDatabase
    {

        private List<User> users;

        public UserDatabase()
        {
            users = new List<User>();

            string passHash = BCrypt.Net.BCrypt.HashPassword("admin123");
            User admin = new User() { Email = "admin@test.com", Username = "admin", PasswordHash = passHash, IsAdmin = true };
            AddUser(admin);

            AddUser(CreatePremadeUser("peterhc01@gmail.com", "Peter", "pass"));
        }
        private User CreatePremadeUser(string email, string username, string password, bool isAdmin = true)
        {
            string passHash = BCrypt.Net.BCrypt.HashPassword(password);
            User nUser = new User() { Email = email, Username = username, PasswordHash = passHash,IsAdmin = isAdmin };
            return nUser;
        }

        public void AddUser(User user)
        {
            users.Add(user);
        }

        public void RemoveUser(User user)
        {
            users.Remove(user);
        }

        public User? GetUserByUsername(string username)
        {
            foreach (var user in users)
            {
                if (user.Username == username) return user;
            }
            return null;
        }

        public User? GetUserByEmail(string email)
        {
            foreach (var user in users)
            {
                if (user.Email == email) return user;
            }
            return null;
        }

        public User? FindUser(UserDto user)
        {
            foreach (User item in users)
            {
                if (item.Username == user.Username && BCrypt.Net.BCrypt.Verify(user.Password, item.PasswordHash))
                    return item;
            }
            return null;
        }

    }
}
