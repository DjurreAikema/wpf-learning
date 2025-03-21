using System.Collections.ObjectModel;

namespace Record_Book_MVVM.Models;

public class UserManager
{
    private static ObservableCollection<User> _DatabaseUsers =
    [
        new User {Email = "quam@aol.couk", Name = "Erich Fry"}, new User {Email = "in@yahoo.com", Name = "Virginia Salas"},
        new User {Email = "luctus@google.com", Name = "Alvin Velasquez"}, new User {Email = "etiam.bibendum@hotmail.edu", Name = "Zia Hutchinson"},
        new User {Email = "augue.porttitor@protonmail.org", Name = "Justin Burch"}
    ];

    public static ObservableCollection<User> GetUsers() => _DatabaseUsers;

    public static void AddUser(User user) => _DatabaseUsers.Add(user);
}