using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CinemaBot.Services.Interfaces
{
    public interface ITelegramService
    {
        Task<ITelegramBotClient> GetBotClientAsync();
        Task SendMessage(Chat chatId, string message);
    }
}