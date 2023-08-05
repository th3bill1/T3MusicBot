using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using KebabBot.Handlers;
using Victoria.Node;
using Discord.Commands;
using System.IO;
using Discord.Interactions;
using Victoria;

namespace KebabBot.Services
{
    public class DiscordService
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionHandler _interactionHandler;
        private readonly ServiceProvider _services;
        private readonly LavaNode _lavaNode;
        private readonly AudioService _audioService;
        private string _token;
       
        public DiscordService()
        {
            _services = ConfigureServices();
            _client = _services.GetRequiredService<DiscordSocketClient>();
            _interactionHandler = _services.GetRequiredService<InteractionHandler>();
            _lavaNode = _services.GetRequiredService<LavaNode>();
            _audioService = _services.GetRequiredService<AudioService>();
            using (StreamReader sr = new("C:\\Program Files\\KebabBot\\discord_token.txt")) _token = sr.ReadToEnd();

            SubscribeDiscordEvents();
        }
        public async Task InitializeAsync()
        {
            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private void SubscribeDiscordEvents()
        {
            _client.Ready += ReadyAsync;
            _client.Log += LogAsync;
        }


        /* Used when the Client Fires the ReadyEvent. */
        private async Task ReadyAsync()
        {
            try
            {
                await _lavaNode.ConnectAsync();
                Console.WriteLine(_lavaNode.IsConnected.ToString());
            }
            catch (Exception ex)
            {
                await LoggingService.LogInformationAsync(ex.Source, ex.Message);
            }

        }

        /*Used whenever we want to log something to the Console. 
            Todo: Hook in a Custom LoggingService. */
        private async Task LogAsync(LogMessage logMessage)
        {
            await LoggingService.LogAsync(logMessage.Source, logMessage.Severity, logMessage.Message);
        }

        /* Configure our Services for Dependency Injection. */
        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddLogging()
                .AddLavaNode()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<InteractionHandler>()
                .AddSingleton<LavaNode>()
                .AddSingleton(new NodeConfiguration())
                .AddSingleton<AudioService>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .BuildServiceProvider();
        }

    }
}