using KebabBot.Services;

namespace KebabBot
{
    public class KebabBotProgram
    {
        class Program
        {
            public static Task Main()
                => new DiscordService().InitializeAsync();
        }
    }
}