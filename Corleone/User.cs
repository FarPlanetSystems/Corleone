namespace Corleone
{
    public class User
    {
        public string Username { get; set; }
        public int Id { get; set; }

        public User(string username, int id)
        {
            this.Username = username;
            this.Id = id;
        }
    }
}
