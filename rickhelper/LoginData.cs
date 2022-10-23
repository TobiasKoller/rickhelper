namespace rickhelper
{
    public class LoginData
    {
        public string User { get; set; }
        public string Password { get; set; }

        public LoginData(string user, string password)
        {
            User = user;
            Password = password;
        }
    }
}
