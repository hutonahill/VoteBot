using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VoteBot.Data;
using VoteBot.Models;
using IResult = Discord.Interactions.IResult;
using JsonException = Newtonsoft.Json.JsonException;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace VoteBot.DiscordBot;

public class Bot : BackgroundService {
    private static DiscordSocketClient _client;
    private static readonly ILogger<Bot> _logger;
    private static string _token;
    private readonly IServiceProvider _services;
    
    
    private static InteractionService? _interactionService;
    
    private static IConfiguration? _configuration;
    
    private static readonly DiscordSocketConfig _socketConfig = new() {
        GatewayIntents = GatewayIntents.GuildMembers | GatewayIntents.MessageContent | GatewayIntents.Guilds | 
                         GatewayIntents.GuildMessages | GatewayIntents.GuildVoiceStates,
        AlwaysDownloadUsers = true,
    };
    
    
    public Bot(IConfiguration config, IServiceProvider services) {
        _token = config["Discord:Token"] ?? throw new InvalidOperationException("Bot token missing");
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        _configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables(prefix: "DC_")
            .AddJsonFile("secrets.json")
            .AddJsonFile("appsettings.json")
            .Build();
        
        _client = new DiscordSocketClient(_socketConfig);
        
        _client.Log += LogInternalAsync;
        _client.Ready += ClientReadyAsync;
        _client.JoinedGuild += HandleGuildJoinedAsync;

        _client.AutocompleteExecuted += HandleAutocompleteExecution;
        
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();

        // Keep running until canceled
        await Task.Delay(-1, stoppingToken);
    }
    
    private async Task HandleGuildJoinedAsync(SocketGuild guild) {
        // add an entry
        using VoteScope scope = GetVoteScope();
        guild.AddServer(scope);

        //check for joe
    }
    
    public static async Task LogAsync(LogSeverity severity, string message, Exception? exception = null,
        [CallerMemberName] string source = "<Unknown>") 
    {
        await LogInternalAsync(new LogMessage(severity, source, message, exception));

    }
    
    private static async Task HandleAutocompleteExecution(SocketAutocompleteInteraction arg) {
        var context = new InteractionContext(_client, arg, arg.Channel);
        Debug.Assert(_interactionService != null, nameof(_interactionService) + " != null");
        await _interactionService.ExecuteCommandAsync(context, null);
    }

    private async Task OnMessageReceivedAsync(SocketMessage message) {
        if (message.Author.IsBot) return;

        if (message.Content == "!ping") {
            await message.Channel.SendMessageAsync("Pong!");
        }
    }
    
    private static async Task HandleSlashCommandExecuted(SlashCommandInfo arg1, IInteractionContext arg2,
        IResult arg3) 
    {
        if (!arg3.IsSuccess) {
            switch (arg3.Error) {
                case InteractionCommandError.UnmetPrecondition:
                    await arg2.Interaction.RespondAsync($"Unmet Precondition: {arg3.ErrorReason}");
                    break;
                case InteractionCommandError.UnknownCommand:
                    await arg2.Interaction.RespondAsync("Unknown command");
                    break;
                case InteractionCommandError.BadArgs:
                    await arg2.Interaction.RespondAsync("Invalid number or arguments");
                    break;
                case InteractionCommandError.Exception:
                    await arg2.Interaction.RespondAsync($"Command exception: {arg3.ErrorReason}");
                    break;
                case InteractionCommandError.Unsuccessful:
                    await arg2.Interaction.RespondAsync("Command could not be executed");
                    break;
            }
        }
    }
    
    private async Task ClientReadyAsync() {

        _interactionService = new InteractionService(_client, new InteractionServiceConfig {
            UseCompiledLambda = true,
            ThrowOnError = true
        });
        
        

        _interactionService.SlashCommandExecuted += HandleSlashCommandExecuted;
        _interactionService.Log += LogInternalAsync;

        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        await _interactionService.RegisterCommandsGloballyAsync();
        
        Debug.Assert(_client != null, nameof(_client) + " != null");
        using VoteScope scope = GetVoteScope();
        await VerifyServer(scope);
        
        Console.WriteLine($"Logged in as {_client.CurrentUser.Username} - {_client.CurrentUser.Id}");
    }
    
    
    
    private static async Task LogInternalAsync(LogMessage log) {

        List<string> messageList = new List<string>();

        string block1 = "## Log: ";

        string block2 = "";

        if (!string.IsNullOrEmpty(log.Source)) {
            block1 += $"\n### Source \n\t`{log.Source}` ";
        }
        
        if (log.Message != String.Empty) {
            block1 += $"\n### Log Message: \n```\n {log.Message} \n``` ";
        }

        

        if (log.Exception != null) {
            block1+= $"\n### Exception Type: \n`{log.Exception.GetType().Name} ` ";
            block1+= $"\n### Exception Message: \n```\n{log.Exception.Message}\n```";
            block2+= $"\n### Callstack: \n```\n{log.Exception.StackTrace}\n``` ";
        }
        
        messageList.Add(block1);
        if (block2 != "") {
            messageList.Add(block2);
        }

        messageList.Add("\n===END LOG===\n"); 
        
        // TODO: Using different sending method for this project. Need to add a new method.
        // Working on learning serial log
        if (channel != null) {
            foreach (string part in messageList) {
                await SendMessageAsync(channel, part)!;
            }
            
            
        }
        else {
            Console.WriteLine("Unable to Identify Channel.");
        }
        

        Console.WriteLine(string.Join("", messageList));
    }
    
    private static async Task SendMessageAsync(ITextChannel channel, string message, int maxMessageLength = 2000) {
        const string codeWrapper = "```";

        if (message.Length < maxMessageLength) {
            await channel.SendMessageAsync(message);
        }
        else {
            
            // check if the message contains code
            if (message.Contains(codeWrapper)) {
                
                // split the block on the code wrapper. 
                // alternating block of code and not code.
                List<string> blocks = message.Split(codeWrapper)
                    .ToList();
                
                // the first block can be either text or code. Let's make a list of methods that handle them 
                List<Func<ITextChannel, string, int, Task>> blockHandlers = [
                    SendMessageAsync,
                    SendCodeBlock
                ];
                
                // now a var to store the current index.
                int handleIndex = 0;
                
                // if the first block is null or empty then the code wrapper must be the first thing in 
                // the message.
                if (string.IsNullOrEmpty(blocks[1])) {
                    handleIndex = 1;
                }
                
                // loop though the blocks alternating type until you run out.
                while (blocks.Count != 0) {
                    
                    string block = blocks.pop(0);
                    
                    // this settles the case of ```<code> ``` ``` <code>```
                    if (!string.IsNullOrEmpty(block)) {
                        await blockHandlers[handleIndex].Invoke(channel, block, maxMessageLength);
                    }
                    
                    // flip the handle index
                    if (handleIndex == 1) {
                        handleIndex = 0;
                    }
                    else {
                        handleIndex = 1;
                    }
                }
            }
        }
    }
    
    private static async Task SendCodeBlock(ITextChannel channel, string code, int maxMessageLength) {
        await SendCodeBlock(channel, code, maxMessageLength, "```");
    }
    
    private static async Task SendCodeBlock(ITextChannel channel, string code, int maxMessageLength, string codeWrapper) {
        maxMessageLength -= (2 * codeWrapper.Length);

        if (code.Length > maxMessageLength) {
            // add lines until we hit or go over the max message length.
            string content = string.Empty;
            
            // let's break up the code into lines.
            List<string> lines = code.Split("\n")
                .ToList();
            
            while (lines.Count > 0) {
                // if one line puts us over, we need to break the line up more. 
                if (lines[0].Length > maxMessageLength && string.IsNullOrEmpty(content)) {
                    
                    string line = lines.pop(0);

                    List<string> sentences = line.Split(". ")
                        .ToList();

                    sentences = sentences.Select(s => s + ". ")
                        .ToList();
                    

                    while (sentences.Count > 0) {
                        // one sentence puts us over.
                        if (string.IsNullOrEmpty(content) && sentences[0].Length > maxMessageLength) {
                            string sentence = sentences.pop(0);

                            List<string> words = sentence.Split(" ")
                                .Select(s => s + " ")
                                .ToList();

                            while (words.Count > 0) {
                                // one word puts us over.
                                if (string.IsNullOrEmpty(content) && words[0].Length > maxMessageLength) {
                                    // fall back to the sentence level and print chars until the sentence is printed.
                                    while (!string.IsNullOrEmpty(sentence)) {
                                        content = sentence[..maxMessageLength];

                                        sentence = sentence[maxMessageLength..];
                                
                                        await SendMessageAsync(channel, $"{codeWrapper}{content}{codeWrapper}");
                                        content = "";
                                    }
                                }
                                
                                // adding one word would exceed limit, send.
                                else if (content.Length + words[0].Length > maxMessageLength) {
                                    await SendMessageAsync(channel, $"{codeWrapper}{content}{codeWrapper}");
                                    content = "";
                                }
                                
                                // add a word
                                else {
                                    content += words.pop(0);
                                }
                            }
                            
                            
                        }
                        // adding would exceed, send
                        else if (content.Length + sentences[0].Length > maxMessageLength) {
                            await SendMessageAsync(channel, $"{codeWrapper}{content}{codeWrapper}");
                            content = "";
                        }
                        
                        // add
                        else {
                            content = sentences.pop(0);
                        }
                    }
                }
                
                // if adding the next thing will push us over the limit, send and reset.
                else if (content.Length + lines[1].Length > maxMessageLength) {
                    await SendMessageAsync(channel, $"{codeWrapper}{content}{codeWrapper}");
                    content = "";
                }
                
                else {
                    content += "\n" + lines.pop(1);
                }
            }
        }
        else {
            await SendMessageAsync(channel, $"{codeWrapper}{code}{codeWrapper}");
        }
    }

    private VoteScope GetVoteScope() {
        return new VoteScope(_services);
    }
    
    public static async Task VerifyServer(VoteScope scope) {
        IReadOnlyCollection<SocketGuild> guilds = _client.Guilds;
        await using VoteContext Context = scope.Db;
        foreach (SocketGuild guild in guilds) {
            if (Context.Servers.Count(server => server.ServerId == guild.Id) == 0) {
                guild.AddServer(scope);
                await Bot.LogAsync(LogSeverity.Info, $"Added server: {guild.Id}");
            }
        }

        await Context.SaveChangesAsync();
        
    }
}

public class VoteScope : IDisposable{
    public VoteContext Db { get; private set; }


    private IServiceScope _scope;

    public VoteScope(IServiceProvider services) {
        _scope = services.CreateScope();
        Db = _scope.ServiceProvider.GetRequiredService<VoteContext>();
    }

    public void Dispose() {
        _scope.Dispose();
    }
}



public static class Util {
    public static T pop<T>(this List<T> list, int index = -1) {
        T value = list[index];
        list.RemoveAt(index);
        return value;
    }
    
    public static void AddServer(this SocketGuild guild, VoteScope scope) {
        using VoteContext context = scope.Db
        
        int numServers = context.Servers.Count(s => s.ServerId == guild.Id);

        if (numServers == 0) {
            Server newServers = new Server {
                ServerId = guild.Id
            };

            context.Add(newServers);

            context.SaveChanges();
        }
    }
    
    
}


