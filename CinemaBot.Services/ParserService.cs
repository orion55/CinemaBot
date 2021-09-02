using System;
using Microsoft.Extensions.Configuration;

namespace CinemaBot.Services
{
    public class ParserService
    {
        public ParserService(IConfiguration config)
        {
            // Console.WriteLine(string.Join("\n", config.GetSection("urls").Get<string[]>()));
            Console.WriteLine(config["useProxy"]);
        }
    }
}