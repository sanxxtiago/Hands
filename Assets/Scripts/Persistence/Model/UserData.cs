using System;
using System.Collections.Generic;

public class UserData
{
    public string UserId { get; set; }
    public string Name { get; set; }
    public DateTime BirthDate { get; set; }
}

public class UserProfile
{
    public string UserId { get; set; }
    public string Name { get; set; }
}

public class UsersData
{
    public List<UserProfile> Profiles { get; set; } = new();
}

public class ActiveUserData
{
    public string UserId { get; set; }
}
