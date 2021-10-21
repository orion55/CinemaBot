using System;
using System.Threading;
using System.Threading.Tasks;
using CinemaBot.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CinemaBot.Services.Services
{
    public class TelegramService : ITelegramService
    {
        private readonly ILogger _log;
        private ITelegramBotClient _botClient = null;
        private readonly IConfiguration _config;
        public CancellationTokenSource cts;

        public TelegramService(ILogger log, IConfiguration configuration)
        {
            _log = log;
            _config = configuration;
        }

        public async Task<ITelegramBotClient> GetBotClientAsync()
        {
            if (_botClient != null)
                return _botClient;

            _botClient = new TelegramBotClient(_config["TelegramToken"]);
            var me = await _botClient.GetMeAsync();

            cts = new CancellationTokenSource();
            _botClient.StartReceiving(
                new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync),
                cts.Token);
            
            _log.Information("Start listening for @{0}", me.Username);
            // cts.Cancel();
            return _botClient;
        }

        Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException =>
                    $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            _log.Error(ErrorMessage);
            return Task.CompletedTask;
        }

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type != UpdateType.Message)
                return;
            if (update.Message.Type != MessageType.Text)
                return;

            var chatId = update.Message.Chat.Id;

            Console.WriteLine($"Received a '{update.Message.Text}' message in chat {chatId}.");

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "You said:\n" + update.Message.Text
            );
        }
        
        public async Task SendMessage(Chat chatId, string message)
        {
            await _botClient.SendPhotoAsync(
                chatId: 311189536,
                photo: "https://github.com/TelegramBots/book/raw/master/src/docs/photo-ara.jpg",
                caption: "<b>Ara bird</b>. <i>Source</i>: <a href=\"https://pixabay.com\">Pixabay</a>",
                parseMode: ParseMode.Html
            );
        }
    }
}