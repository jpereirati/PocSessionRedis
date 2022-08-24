using Bogus;

namespace PocSessionRedis.Api.Model;

public class User
{
    public Guid Id { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public DateTime LoggedAt { get; set; }

    public static List<User> GenerateUsers(int totalUsers = 10)
    {
        var users = new List<User>();

        for (int i = 0; i < totalUsers; i++)
        {
            users.Add(new Faker<User>(locale: "pt_BR")
             .RuleFor(u => u.Id, f => f.Random.Uuid())
             .RuleFor(u => u.UserName, f => f.Person.UserName.ToLower())
             .RuleFor(u => u.Password, "poc")
             .Generate());
        }

        return users;
    }
}
