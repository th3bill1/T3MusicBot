using KebabBot;
using Discord;
using Discord.Interactions;
using System;
using System.Threading.Tasks;
using KebabBot.ChatGPT;
using Discord.Audio;
using System.Diagnostics;

namespace KebabBot.Modules
{
    public class TextModule : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Commands { get; set; }

        private InteractionHandler _handler;

        public TextModule(InteractionHandler handler)
        {
            _handler = handler;
        }

        [SlashCommand("echo", "echo echo echo")]
        public async Task Echo(string echo)
            => await RespondAsync(echo);
        [SlashCommand("mati", "opinia o Matim")]
        public async Task Echo()
            => await RespondAsync("frajer");


        [SlashCommand("ping", "Pinguje i sprawdza opóźnienie")]
        public async Task GreetUserAsync()
            => await RespondAsync(text: $":ping_pong: Zajęło {Context.Client.Latency}ms do odpowiedzi!", ephemeral: true);

       


        [SlashCommand("gpt", "promptuje chat-gpt")]
        public async Task GPTPrompt(string text)
        {
            var msg = await Context.Channel.SendMessageAsync("Daj mi chwile");
            IOpenAIProxy chatOpenAI = new OpenAIProxy(apiKey: "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", organizationId: "");
            var results = await chatOpenAI.SendChatMessage(text);

            var respond = "";
            foreach (var item in results)
            {
                respond += item.Content;
            }
            await msg.ModifyAsync(msg => msg.Content = respond);
        }


        [UserCommand("zwyzywaj")]
        public async Task GreetUserAsync(IUser user)
            => await RespondAsync(text: $"{user.Mention} to chuj \n ~{Context.User.Mention}");

        [MessageCommand("pin")]
        public async Task PinMessageAsync(IMessage message)
        {
            if (message is not IUserMessage userMessage)
                await RespondAsync(text: ":x: Takich nie wolno!");

            else if ((await Context.Channel.GetPinnedMessagesAsync()).Count >= 50)
                await RespondAsync(text: ":x: Osiągnięto limit!");

            else
            {
                await userMessage.PinAsync();
                await RespondAsync(":white_check_mark: Gitara!");
            }
        }
    }
}
