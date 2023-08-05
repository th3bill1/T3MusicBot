using KebabBot.Services;

public class KebabBotProgram
{
    class Program
    {
        private static Task Main()
            => new DiscordService().InitializeAsync();
    }
}