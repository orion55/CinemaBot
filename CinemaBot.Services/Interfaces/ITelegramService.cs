using System.Collections.Generic;
using System.Threading.Tasks;
using CinemaBot.Models;
using Telegram.Bot;

namespace CinemaBot.Services.Interfaces
{
    public interface ITelegramService
    {
        Task<ITelegramBotClient> GetBotClientAsync();
        Task SendMessageMovies(List<UrlModel> links);
    }
}