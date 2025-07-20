using Discord;
using Discord.WebSocket;

namespace VoteBot.DiscordBot;

public class Bot : BackgroundService {
    private readonly DiscordSocketClient _client;
    private readonly ILogger<Bot> _logger;
    private readonly string _token;

    public Bot(ILogger<Bot> logger, IConfiguration config) {
        _logger = logger;
        _token = config["Discord:Token"] ?? throw new InvalidOperationException("Bot token missing");

        _client = new DiscordSocketClient(new DiscordSocketConfig {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        });

        _client.Log += log => {
            _logger.LogInformation(log.ToString());
            return Task.CompletedTask;
        };

        _client.Ready += () => {
            _logger.LogInformation($"Connected as {_client.CurrentUser}");
            return Task.CompletedTask;
        };

        _client.MessageReceived += OnMessageReceivedAsync;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();

        // Keep running until canceled
        await Task.Delay(-1, stoppingToken);
    }

    private async Task OnMessageReceivedAsync(SocketMessage message) {
        if (message.Author.IsBot) return;

        if (message.Content == "!ping") {
            await message.Channel.SendMessageAsync("Pong!");
        }
    }
}