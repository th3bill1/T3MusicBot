using Discord;
using Discord.Commands;
using Discord.Interactions;
using KebabBot.Services;
using Victoria;
using Victoria.Rest.Search;

namespace KebabBot.Modules
{
    public class AudioModule(
    LavaNode<LavaPlayer<LavaTrack>, LavaTrack> lavaNode,
    AudioService audioService)
    : InteractionModuleBase<SocketInteractionContext>
    {
        private static readonly IEnumerable<int> Range = Enumerable.Range(1900, 2000);
        private static int volume = 10;

        [SlashCommand("leave", "Opuszcza kanał głosowy")]
        public async Task LeaveAsync()
        {
            var voiceChannel = (Context.User as IVoiceState).VoiceChannel;
            if (voiceChannel == null)
            {
                await RespondAsync("Nie wiem jaki kanał głosowy opuścić");
                return;
            }

            try
            {
                await lavaNode.LeaveAsync(voiceChannel);
                await RespondAsync($"Opusczam kanał: {voiceChannel.Name}!");
            }
            catch (Exception exception)
            {
                await RespondAsync(exception.Message);
            }
        }
        [SlashCommand("play", "Puść se nutke wariacie")]
        public async Task PlayAsync([Remainder] string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                await RespondAsync("Musisz uzupełnić pole wyszukiwania");
                return;
            }
            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
            if (player == null)
            {
                var voiceState = Context.User as IVoiceState;
                if (voiceState?.VoiceChannel == null)
                {
                    await RespondAsync("Musisz być na kanale głosowym!");
                    return;
                }

                try
                {
                    player = await lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
                    await ReplyAsync($"Dołączam do: {voiceState.VoiceChannel.Name}!");
                    await player.SetVolumeAsync(lavaNode, volume);
                }
                catch (Exception exception)
                {
                    await ReplyAsync(exception.Message);
                }
            }
            var searchResponse = await lavaNode.LoadTrackAsync(searchQuery);
            switch (searchResponse.Type)
            {
                case SearchType.Empty:
                case SearchType.Error:
                    await ReplyAsync($"Nie mogłem puścić nic dla `{searchQuery}`.");
                    return;
                case SearchType.Track:
                    var t1 = searchResponse.Tracks.FirstOrDefault();
                    player.GetQueue().Enqueue(t1);
                    await RespondAsync($"Dodałem {t1?.Title} do kolejki.");
                    break;
                    case SearchType.Playlist:
                    foreach (var t2 in searchResponse.Tracks)
                    {
                        player.GetQueue().Enqueue(t2);
                    }
                    await RespondAsync($"Dodałem do kolejki {searchResponse.Tracks.Count} utworów.");
                    break;

            }
            player.GetQueue().TryDequeue(out var queueable);
            await player.PlayAsync(lavaNode, queueable);
            await player.SetVolumeAsync(lavaNode, volume);

        }
        [SlashCommand("volume", "Zmienia głośność")]
        public async Task Volume(int _volume)
        {
            volume = _volume;
            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
            if (player == null)
            {
                await RespondAsync("Nie jestem na żadnym kanale głosowym!");
                return;
            }
            await player.SetVolumeAsync(lavaNode, volume);
            await RespondAsync($"Zmieniono głośność na {volume}.");
        }
        [SlashCommand("skip", "skipuje nutke")]
        public async Task Skip()
        {
            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
            if (player == null)
            {
                await RespondAsync("Nie jestem na żadnym kanale głosowym!");
                return;
            }
            var a = await player.SkipAsync(lavaNode);
            await RespondAsync("Pominięto utwór.");
        }
        [SlashCommand("current_volume", "wyświetla obecny poziom głośności")]
        public async Task CurrVolume()
        {
            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
            if (player == null)
            {
                await RespondAsync("Nie jestem na żadnym kanale głosowym!");
                return;
            }
            else await RespondAsync($"Obecna głośność: {player.Volume}");
        }
        [SlashCommand("playlist","wyświetla listę utworów w kolejce")]
        public async Task Playlist()
        {
            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
            if (player == null)
            {
                var voiceState = Context.User as IVoiceState;
                if (voiceState?.IsStreaming == false)
                {
                    await RespondAsync("Nic obecnie nie gra!");
                    return;
                }
            }
            var count = 1;
            var response = "";
            await RespondAsync("Playlista:");
            foreach (var song in player.GetQueue())
            {
                response += $"{count}. {song.Title}\n";
                count++;
            }
            await ReplyAsync(response);
        }
        [SlashCommand("remove", "usuwa piosenkę z danej pozycji w kolejce")]
        public async Task RemoveSong(int position)
        {
            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
            if (player == null)
            {
                await RespondAsync("Nie jestem na żadnym kanale głosowym!");
                return;
            }
            else if (position > player.Queue.Count)
            {
                await RespondAsync("Wybrana pozycja jest mniejsza niż liczba pozycji w kolejce!");
                return;
            }
            else if(position < 0) 
            {
                await RespondAsync("Wybrana pozycja nie może być mniejsza niż 0!");
                return;
            }
            else
            {
                player.Queue.RemoveAt(position-1);
                await RespondAsync($"Usunięto utwór na pozycji {position}.");
            }
        }

        [SlashCommand("tekst", "Tekst do wybranej piosenki (domyślnie obecnie grająca)")]
        public async Task Lyrics(string tytuł = null)
        {
            if (tytuł == null)
            {
                var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
                if (player == null || !player.State.IsConnected)
                {
                    await RespondAsync("Nic nie gram i nie wpisano tytułu piosenki!");
                    return;
                }
                else tytuł = player.Track.Title;
            }
            await RespondAsync($"Tekst do piosenki {tytuł}");
            var msg = await Context.Channel.SendMessageAsync("Pobieranie tekstu piosenki...");
            var geniusservce = new GeniusService();
            var lyrics = geniusservce.GetSongLyrics(tytuł).Result;
            if (lyrics.Length > 2000)
            {
                var index = lyrics.Substring(1500).IndexOf('\n');
                await msg.ModifyAsync(msg => msg.Content = lyrics[..(index + 1500)]);
                await ReplyAsync(lyrics[(index + 1500)..]);
            }
            else await msg.ModifyAsync(msg => msg.Content = lyrics);
        }
        [SlashCommand("pause","pauzuje muzyczke")]
        public async Task Pause()
        {
            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
            if (player == null)
            {
                var voiceState = Context.User as IVoiceState;
                if (voiceState?.IsStreaming == false)
                {
                    await RespondAsync("Nic obecnie nie gra!");
                    return;
                }
            }
            player.GetQueue().EnqueueFirst(player.Track);
            await player.PauseAsync(lavaNode);
            await RespondAsync("Zapauzowano muzykę!");
        }
        [SlashCommand("resume", "wznów muzyczke")]
        public async Task Resume()
        {
            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
            if (player == null)
            {
                var voiceState = Context.User as IVoiceState;
                if (voiceState?.IsStreaming == false)
                {
                    await RespondAsync("Nie było pauzowane!");
                    return;
                }
            }
            await player.ResumeAsync(lavaNode,player.GetQueue().First());
            await RespondAsync("Wznowiono muzykę!");
        }

    }
}
