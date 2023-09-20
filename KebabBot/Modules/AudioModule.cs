using Discord;
using Discord.Commands;
using Victoria.Node;
using Victoria.Player;
using Victoria.Responses.Search;
using Discord.Interactions;
using KebabBot.Services;
using System.Text;
using Victoria;

namespace KebabBot.Modules
{
    public class AudioModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly LavaNode _lavaNode;
        private readonly AudioService _audioService;
        private static readonly IEnumerable<int> Range = Enumerable.Range(1900, 2000);
        private int _volume = 10;

        private AudioModule(LavaNode lavaNode, AudioService audioService)
        {
            _lavaNode = lavaNode;
            _audioService = audioService;
        }
        [SlashCommand("leave", "Opuszcza kanał głosowy")]
        public async Task LeaveAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await RespondAsync("Nie jestem na żadnym kanale głosowym!");
                return;
            }

            var voiceChannel = (Context.User as IVoiceState).VoiceChannel ?? player.VoiceChannel;
            if (voiceChannel == null)
            {
                await RespondAsync("Nie wiem jaki kanał głosowy opuścić");
                return;
            }

            try
            {
                await _lavaNode.LeaveAsync(voiceChannel);
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

            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                var voiceState = Context.User as IVoiceState;
                if (voiceState?.VoiceChannel == null)
                {
                    await RespondAsync("Muszę być na kanale głosowym!");
                    return;
                }

                try
                {
                    player = await _lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
                    await player.SetVolumeAsync(_volume);
                    await ReplyAsync($"Dołączam do: {voiceState.VoiceChannel.Name}!");
                }
                catch (Exception exception)
                {
                    await RespondAsync(exception.Message);
                }
            }
            var searchResponse = await _lavaNode.SearchAsync(Uri.IsWellFormedUriString(searchQuery, UriKind.Absolute) ? SearchType.Direct : SearchType.YouTube, searchQuery);
            if (searchResponse.Status is SearchStatus.LoadFailed or SearchStatus.NoMatches)
            {
                await ReplyAsync($"Nie znalazłem nic o nazwie: `{searchQuery}`.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
            {
                player.Vueue.Enqueue(searchResponse.Tracks);
                await RespondAsync($"Dodałem do kolejki {searchResponse.Tracks.Count} utworów.");
            }
            else
            {
                var track = searchResponse.Tracks.FirstOrDefault();
                player.Vueue.Enqueue(track);
                await RespondAsync($"Dodałem {track?.Title} do kolejki.");
            }

            if (player.PlayerState is PlayerState.Playing or PlayerState.Paused)
            {
                return;
            }

            player.Vueue.TryDequeue(out var lavaTrack);
            await player.PlayAsync(lavaTrack);
            await player.SetVolumeAsync(_volume);

        }
        [SlashCommand("volume", "Zmienia głośność")]
        public async Task Volume(int volume)
        {
            _volume = volume;
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await RespondAsync("Nie jestem na żadnym kanale głosowym!");
                return;
            }
            await player.SetVolumeAsync(volume);
            await RespondAsync($"Zmieniono głośność na {volume}.");
        }
        [SlashCommand("skip", "skipuje nutke")]
        public async Task Skip()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await RespondAsync("Nie jestem na żadnym kanale głosowym!");
                return;
            }
            else if(player.Vueue.Count == 0)
            {
                await player.StopAsync();
            }
            else await player.SkipAsync();

            await player.SetVolumeAsync(_volume);
            await RespondAsync("Pominięto utwór.");
        }
        [SlashCommand("current_volume", "wyświetla obecny poziom głośności")]
        public async Task CurrVolume()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await RespondAsync("Nie jestem na żadnym kanale głosowym!");
                return;
            }
            else await RespondAsync($"Obecna głośność: {player.Volume}");
        }
        [SlashCommand("playlist","wyświetla listę utworów w kolejce")]
        public async Task Playlist()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
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
            foreach (var song in player.Vueue)
            {
                response += $"{count}. {song.Title}\n";
                count++;
            }
            await ReplyAsync(response);
        }
        [SlashCommand("remove", "usuwa piosenkę z danej pozycji w kolejce")]
        public async Task RemoveSong(int position)
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await RespondAsync("Nie jestem na żadnym kanale głosowym!");
                return;
            }
            else if (position > player.Vueue.Count)
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
                player.Vueue.RemoveAt(position-1);
                await RespondAsync($"Usunięto utwór na pozycji {position}.");
            }
        }

        [SlashCommand("tekst", "Tekst do wybranej piosenki (domyślnie obecnie grająca)")]
        public async Task Lyrics(string tytuł = null)
        {
            if (tytuł == null)
            {
                if ((!_lavaNode.TryGetPlayer(Context.Guild, out var player)) || player.PlayerState != PlayerState.Playing)
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
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                var voiceState = Context.User as IVoiceState;
                if (voiceState?.IsStreaming == false)
                {
                    await RespondAsync("Nic obecnie nie gra!");
                    return;
                }
            }
            await player.PauseAsync();
            await RespondAsync("Zapauzowano muzykę!");
        }
        [SlashCommand("resume", "wznów muzyczke")]
        public async Task Resume()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                var voiceState = Context.User as IVoiceState;
                if (voiceState?.IsStreaming == false)
                {
                    await RespondAsync("Nie było pauzowane!");
                    return;
                }
            }
            await player.ResumeAsync();
            await RespondAsync("Wznowiono muzykę!");
        }

    }
}
