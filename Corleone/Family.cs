using System.Globalization;
using System.Text.Json;

namespace Corleone
{
    public class Family
    {
        private static bool isInitialized = false;
        private int lastId = 0;

        private readonly string password;

        private Family(string password)  {
            this.password = password;
        }

        public bool RegisterUser(string username, string password)
        {
            if (this.password != password) {
                Console.WriteLine("Wrong password");
                return false;
            }
            try
            {
                User user = new User(username, lastId);
                User[] users = JsonSerializer.Deserialize<User[]>(File.ReadAllText("Data/members.json")) ?? Array.Empty<User>();
                if (users.Any(u => u.Username == username)) {
                    Console.WriteLine("Username already exists");
                    return true;
                }
                users = users.Append(user).ToArray();
                File.WriteAllText("Data/members.json", JsonSerializer.Serialize(users));
                lastId++;
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                return false;
            }
            
            return true;
        }

        public bool MemberExists(string username) {
            User[] users = JsonSerializer.Deserialize<User[]>(File.ReadAllText("Data/members.json")) ?? Array.Empty<User>();
            if (users.Any(u => u.Username == username))
            {
                return true;
            }
            return false;
        }

        public bool AuthenticateMember(string name, string password)
        {
            if (MemberExists(name) && this.password == password)
            {
                return true;
            }
            return false;
        }

        public static Family createSingletonFamily(string password) {
            if (!isInitialized) {
                isInitialized = true;
                return new Family(password);
            }
            return null;
        }
    }
}
