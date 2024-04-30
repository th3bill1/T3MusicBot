using System.Collections.Concurrent;
using System.Text.Json;
using Discord;
using Microsoft.Extensions.Logging;
using Victoria;
using Victoria.WebSocket.EventArgs;
using Victoria.Enums;
namespace KebabBot.Services
{
    public class AudioService
    {
        private readonly LavaNode<LavaPlayer<LavaTrack>, LavaTrack> _lavaNode;
        private readonly ILogger _logger;
        public readonly HashSet<ulong> VoteQueue;
        private readonly ConcurrentDictionary<ulong, CancellationTokenSource> _disconnectTokens;

        public AudioService(LavaNode<LavaPlayer<LavaTrack>, LavaTrack> lavaNode, ILoggerFactory loggerFactory)
        {
            _lavaNode = lavaNode;
            _logger = loggerFactory.CreateLogger<LavaNode<LavaPlayer<LavaTrack>, LavaTrack>>();
            _disconnectTokens = new ConcurrentDictionary<ulong, CancellationTokenSource>();

            VoteQueue = new HashSet<ulong>();

            _lavaNode.OnTrackEnd += OnTrackEndAsync;
            _lavaNode.OnTrackStart += OnTrackStartAsync;
            _lavaNode.OnStats += OnStatsReceivedAsync;
            _lavaNode.OnWebSocketClosed += OnWebSocketClosedAsync;
        }


        private Task OnWebSocketClosedAsync(WebSocketClosedEventArg arg)
        {
            _logger.LogCritical($"{arg.Code} {arg.Reason}");
            return Task.CompletedTask;
        }

        private Task OnStatsReceivedAsync(StatsEventArg arg)
        {
            _logger.LogInformation(JsonSerializer.Serialize(arg));
            return Task.CompletedTask;
        }

        private Task OnTrackStartAsync(TrackStartEventArg arg)
        {
            var players = _lavaNode.GetPlayersAsync().Result;
            //return players.First().TextChannel.SendMessageAsync($"Puszczam nutke: {arg.Track.Title}.");
            return Task.CompletedTask;
        }

        private async Task OnTrackEndAsync(TrackEndEventArg arg)
        {
            if (arg.Reason!=TrackEndReason.Finished)
            {
                return;
            }
            LavaPlayer<LavaTrack> player = await _lavaNode.TryGetPlayerAsync(arg.GuildId);
            player.GetQueue().TryDequeue(out var queueable);
            if (queueable == null)
            {
                //await player.TextChannel.SendMessageAsync("Koniec piosenek w kolejce!");
                return;
            }
            if (queueable is not LavaTrack track)
            {
                //await player.TextChannel.SendMessageAsync("Następne element w kolejce nie jest piosenką!");
                return;
            }
            var volume = player.Volume;
            await player.PlayAsync(_lavaNode, track);
            await player.SetVolumeAsync(_lavaNode,volume);
            
            //if(arg.Reason==TrackEndReason.Finished) await arg.Player.TextChannel.SendMessageAsync($"Koniec {arg.Track.Title}");
            //if (arg.Reason == TrackEndReason.Stopped) await arg.Player.TextChannel.SendMessageAsync($"Pominięto {arg.Track.Title}");

        }
    }
}
