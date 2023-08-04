using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using KebabBot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Victoria;
using Victoria.Node;

public class KebabBotProgram
{

    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _services;
    private string _token;

    private readonly DiscordSocketConfig _socketConfig = new()
    {
        GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers,
        AlwaysDownloadUsers = true,
    };

    public KebabBotProgram()
    {
        _configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables(prefix: "")
            .Build();

        _services = new ServiceCollection()
            .AddLogging()
            .AddLavaNode()
            .AddSingleton<MusicManager>()
            .AddSingleton(_configuration)
            .AddSingleton(_socketConfig)
            .AddSingleton<LavaNode>()
            .AddSingleton<AudioService>()
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .AddSingleton<InteractionHandler>()
            .BuildServiceProvider();
    }

    static void Main(string[] args) => new KebabBotProgram().RunAsync().GetAwaiter().GetResult();

    public async Task RunAsync()
    {
        var client = _services.GetRequiredService<DiscordSocketClient>();

        client.Log += LogAsync;

        await _services.GetRequiredService<InteractionHandler>()
            .InitializeAsync();

        using (StreamReader sr = new("C:\\Program Files\\KebabBot\\token.txt")) _token =  sr.ReadToEnd();

        await client.LoginAsync(TokenType.Bot, _token);
        await client.StartAsync();

        await Task.Delay(Timeout.Infinite);
    }

    private async Task LogAsync(LogMessage message)
        => Console.WriteLine(message.ToString());

}
