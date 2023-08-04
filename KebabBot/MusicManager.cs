using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria.Node;
using Victoria.Player;
using Victoria;

namespace KebabBot
{
    internal class MusicManager
    {
        private readonly LavaNode _lavaNode;

        public MusicManager(DiscordSocketClient socketClient, LavaNode lavaNode)
        {
            socketClient.Ready += OnReady;
            _lavaNode = lavaNode;
            //_lavaNode.OnTrackEnded += OnTrackEnded;

        }

        /*private async Task OnTrackEnded(TrackEndedEventArgs args)
        {
            if (!args.Reason.ShouldPlayNext())
                return;

            var player = args.Player;
            if (!player.Queue.TryDequeue(out var queueable))
            {
                await player.TextChannel.SendMessageAsync("No more tracks to play.");
                return;
            }

            if (!(queueable is LavaTrack track))
            {
                await player.TextChannel.SendMessageAsync("Next item in queue is not a track.");
                return;
            }

            await args.Player.PlayAsync(track);
            await args.Player.TextChannel.SendMessageAsync($"{args.Reason}: {args.Track.Title}\nNow playing: {track.Title}");
        }*/

        private async Task OnReady()
        {
            if (!_lavaNode.IsConnected)
                await _lavaNode.ConnectAsync();
        }
    }
}
