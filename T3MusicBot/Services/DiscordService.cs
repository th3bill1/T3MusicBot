﻿using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using KebabBot.Handlers;
using Discord.Commands;
using Discord.Interactions;
using Victoria;
using Microsoft.Extensions.Logging;

namespace KebabBot.Services
{
    public class DiscordService
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionHandler _interactionHandler;
        private readonly ServiceProvider _services;
        private readonly LavaNode<LavaPlayer<LavaTrack>,LavaTrack> _lavaNode;
        private readonly AudioService _audioService;
        private string _token;
        private string lavalink_hostname;
        private string lavalink_port;
       
        public DiscordService()
        {
            _services = ConfigureServices();
            _client = _services.GetRequiredService<DiscordSocketClient>();
            _interactionHandler = _services.GetRequiredService<InteractionHandler>();
            _lavaNode = _services.GetRequiredService<LavaNode<LavaPlayer<LavaTrack>, LavaTrack>>();
            _audioService = _services.GetRequiredService<AudioService>();
            using (StreamReader sr = new("C:\\Program Files\\KebabBot\\discord_token.txt")) _token = sr.ReadToEnd();

            SubscribeDiscordEvents();
        }
        public async Task InitializeAsync()
        {
            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();

            await _interactionHandler.InitializeAsync();

            await Task.Delay(-1);
        }

        private void SubscribeDiscordEvents()
        {
            _client.Ready += ReadyAsync;
            _client.Log += LogAsync;
        }

        private async Task ReadyAsync()
        {
            try
            {
                await _lavaNode.ConnectAsync();
            }
            catch (Exception ex)
            {
                await LoggingService.LogInformationAsync(ex.Source, ex.Message);
            }

        }

        private async Task LogAsync(LogMessage logMessage)
        {
            await LoggingService.LogAsync(logMessage.Source, logMessage.Severity, logMessage.Message);
        }

        private ServiceProvider ConfigureServices()
        {
            using (StreamReader sr = new("C:\\Program Files\\KebabBot\\lavalink.txt"))
            {
                lavalink_hostname = sr.ReadLine();
                lavalink_port = sr.ReadLine();
            }


            return new ServiceCollection()
                .AddLavaNode()
                .AddLogging()
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Debug,
                    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
                }))
                .AddSingleton<CommandService>()
                .AddSingleton<InteractionHandler>()
                .AddSingleton(new Configuration() { Hostname = lavalink_hostname, Port = Convert.ToUInt16(lavalink_port) })
                .AddSingleton<AudioService>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .BuildServiceProvider();
        }

    }
}