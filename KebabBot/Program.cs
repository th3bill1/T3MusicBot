using KebabBot.Services;

public class KebabBotProgram
{
    class Program
    {
        public static Task Main()
            => new DiscordService().InitializeAsync();
    }
}