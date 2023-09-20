using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Victoria.Node;
using Victoria.Node.EventArgs;
using Victoria.Player;

namespace KebabBot.Services
{
    public class AudioService
    {
        private readonly LavaNode _lavaNode;
        private readonly ILogger _logger;
        public readonly HashSet<ulong> VoteQueue;
        private readonly ConcurrentDictionary<ulong, CancellationTokenSource> _disconnectTokens;

        public AudioService(LavaNode lavaNode, ILoggerFactory loggerFactory)
        {
            _lavaNode = lavaNode;
            _logger = loggerFactory.CreateLogger<LavaNode>();
            _disconnectTokens = new ConcurrentDictionary<ulong, CancellationTokenSource>();

            VoteQueue = new HashSet<ulong>();

            _lavaNode.OnTrackEnd += OnTrackEndAsync;
            _lavaNode.OnTrackStart += OnTrackStartAsync;
            _lavaNode.OnStatsReceived += OnStatsReceivedAsync;
            _lavaNode.OnWebSocketClosed += OnWebSocketClosedAsync;
            _lavaNode.OnTrackStuck += OnTrackStuckAsync;
            _lavaNode.OnTrackException += OnTrackExceptionAsync;
        }

        private static Task OnTrackExceptionAsync(TrackExceptionEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg)
        {
            arg.Player.Vueue.Enqueue(arg.Track);
            return arg.Player.TextChannel.SendMessageAsync($"{arg.Track} rzucił wyjątek :(");
        }

        private static Task OnTrackStuckAsync(TrackStuckEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg)
        {
            arg.Player.Vueue.Enqueue(arg.Track);
            return arg.Player.TextChannel.SendMessageAsync($"{arg.Track} utknął w pralce");
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

        private static Task OnTrackStartAsync(TrackStartEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg)
        {
            return arg.Player.TextChannel.SendMessageAsync($"Puszczam nutke: {arg.Track.Title}.");
        }

        private static async Task OnTrackEndAsync(TrackEndEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg)
        {
            if (arg.Reason!=Victoria.Player.TrackEndReason.Finished)
            {
                return;
            }
            var player = arg.Player;
            if (!player.Vueue.TryDequeue(out var queueable))
            {
                await player.TextChannel.SendMessageAsync("Koniec piosenek w kolejce!");
                return;
            }

            if (queueable is not LavaTrack track)
            {
                await player.TextChannel.SendMessageAsync("Następne element w kolejce nie jest piosenką!");
                return;
            }
            var volume = player.Volume;
            await arg.Player.PlayAsync(track);
            await arg.Player.SetVolumeAsync(volume);
            
            if(arg.Reason==TrackEndReason.Finished) await arg.Player.TextChannel.SendMessageAsync($"Koniec {arg.Track.Title}");
            if (arg.Reason == TrackEndReason.Stopped) await arg.Player.TextChannel.SendMessageAsync($"Pominięto {arg.Track.Title}");

        }
    }
}
