namespace CinemaBot.Services.Services
{
    public class Proxy
    {
        string proxyHost;
        int proxyPort;
        string userId;
        string password;

        public Proxy(string proxyHost, int proxyPort, string userId, string password)
        {
            this.proxyHost = proxyHost;
            this.proxyPort = proxyPort;
            this.userId = userId;
            this.password = password;
        }
    }
}