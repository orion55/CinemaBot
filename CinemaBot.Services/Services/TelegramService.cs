using CinemaBot.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace CinemaBot.Services.Services
{
    public class TelegramService: ITelegramService
    {
        private readonly ILogger _log;
        
        public TelegramService(ILogger log, IConfiguration configuration)
        {
            _log = log;
        }
    }
}