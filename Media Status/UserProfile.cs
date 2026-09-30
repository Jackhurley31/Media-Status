using System.Collections.Generic;

namespace Media_Status
{
    public class UserProfile
    {
        public string Username { get; set; }
        public bool IsDefault { get; set; }
    }

    public class UserDatabase
    {
        public List<UserProfile> Users { get; set; } = new List<UserProfile>();
        public string ActiveUser { get; set; }
    }
}