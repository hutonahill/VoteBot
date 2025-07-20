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
    private static readonly DiscordSocketClient _client;
    private static readonly ILogger<Bot> _logger;
    private static readonly string _token;
    
    
    private static InteractionService? _interactionService;
    
    private static IConfiguration? _configuration;
    
    private static readonly DiscordSocketConfig _socketConfig = new() {
        GatewayIntents = GatewayIntents.GuildMembers | GatewayIntents.MessageContent | GatewayIntents.Guilds | 
                         GatewayIntents.GuildMessages | GatewayIntents.GuildVoiceStates,
        AlwaysDownloadUsers = true,
    };

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
    
    private static async Task HandleGuildJoinedAsync(SocketGuild guild) {
        // add an entry
        guild.AddServer();

        //check for joe
        if (config != null) {
            SocketUser? Joe = guild.GetUser(config.JoeUserId);

            if (Joe != null) {
                SocketTextChannel defaultChannel = guild.DefaultChannel;

                if (defaultChannel is ITextChannel textChannel) {
                    // Send a welcome message to the default channel
                    await textChannel.SendMessageAsync($"oof");
                    await Task.Delay(1000);

                    await textChannel.SendMessageAsync("Well...");
                    await Task.Delay(1000);

                    await textChannel.SendMessageAsync("This is awkward...");
                    await Task.Delay(700);

                    await textChannel.SendMessageAsync(Joe.Mention);
                }
                else {
                    await LogAsync(LogSeverity.Info, 
                        "Default channel is not a text channel or does not exist.");
                }
            }
        }
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
    
    private static async Task ClientReadyAsync() {

        _interactionService = new InteractionService(_client, new InteractionServiceConfig {
            UseCompiledLambda = true,
            ThrowOnError = true
        });
        
        

        _interactionService.SlashCommandExecuted += HandleSlashCommandExecuted;
        _interactionService.Log += LogInternalAsync;

        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        await _interactionService.RegisterCommandsGloballyAsync();
        
        Debug.Assert(_client != null, nameof(_client) + " != null");
        await _client.VerifyServer();
        
        Console.WriteLine($"Logged in as {_client.CurrentUser.Username} - {_client.CurrentUser.Id}");
    }
    
    private static async Task LogInternalAsync(LogMessage log) {
        
        Debug.Assert(config != null, nameof(config) + " != null");
        Debug.Assert(_client != null, nameof(_client) + " != null");
        ITextChannel? channel = _client.GetChannel(config.LoggingChannel) as ITextChannel;

        List<string> messageList = new List<string>();
        
        if (log.Severity == LogSeverity.Critical) {
            SocketUser admin = _client.GetUser(config.AdminUser);

            messageList.Add("{admin.Mention}: \n");
        }

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
}



public static class Util {
    public static T pop<T>(this List<T> list, int index = -1) {
        T value = list[index];
        list.RemoveAt(index);
        return value;
    }
    
    public static void AddServer(this SocketGuild guild, VoteContext context) {
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


//TODO: remember how all this works
public class DiscordOptions {
    
    public const string SectionName = "BotSettings";
    
    public Dictionary<string, ISetting?> GlobalSettings { get; set; } = new() {
        { "NsfwFlow", new Setting<bool>("Flow is considered NSFW", true) },
        { "ScoreRoundsTo", new Setting<uint>("Round scores to this place", 10) }
    };
    

    // my user_id
    public ulong AdminUser { get; set; } = 168496575369183232;

    public string DISCORD_KEY { get; set; } = "DISCORD API KEY";

    public string pasteBinUsername { get; set; } = "USERNAME";

    public string pasteBinPassword { get; set; } = "PASSWORD";

    public string pasteApiKey { get; set; } = "PASTE API KEY";

    public string flowPasteKey { get; set; } = "taDVgTGF";

    public string jokePasteKey { get; set; } = "LHU6giXy";

    public List<ulong> BlacklistedUsers { get; set; } = new();

    //                              my personal server,  The party bus
}


public abstract class ISetting {
    public string? Description { get; init; }
    
    public abstract Type getType();
}

public class Setting<T> : ISetting {
    public T Value { get; set; }

    public override string ToString() {
        return $"\t- {Description}\n" +
               $"\t- Type: '{typeof(T).Name}' \n" +
               $"\t- Value: '{Value}'";
    }

    public override Type getType() {
        return typeof(T);
    }
    
    public Setting(string description, T value) {
        Description = description;
        Value = value;
    }
}

public class SettingBaseConverter : JsonConverter {

    public override bool CanConvert(Type objectType) {
        return typeof(ISetting).IsAssignableFrom(objectType);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
        var settingBase = value as ISetting;

        Debug.Assert(settingBase != null, nameof(settingBase) + " != null");

        writer.WriteStartObject();
        writer.WritePropertyName("Description");
        writer.WriteValue(settingBase.Description);
        writer.WritePropertyName("Type");
        writer.WriteValue(settingBase.GetType().FullName); // Fixed typo to `GetType`
        writer.WritePropertyName("Value");
        serializer.Serialize(writer, ((dynamic)settingBase).Value); // Use dynamic to handle different value types
        writer.WriteEndObject();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) {
        JObject obj = JObject.Load(reader);

        string description = obj["Description"]?.ToString() ?? "";
        string typeName = obj["Type"]?.ToString() ?? "";
        JToken? valueToken = obj["Value"];

        // Resolve the type from the type name
        Type? type = Type.GetType(typeName);
        if (type == null) {
            throw new JsonException($"Unable to find the type: {typeName}");
        }

        // Create a generic Setting<T> type based on the resolved type
        Type settingType = typeof(Setting<>).MakeGenericType(type);
        object? value = valueToken?.ToObject(type, serializer); // Deserialize the value to the expected type

        // Return a new instance of Setting<T> with the deserialized description and value
        return Activator.CreateInstance(settingType, description, value);
    }
}