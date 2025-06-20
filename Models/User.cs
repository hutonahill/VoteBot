using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Discord.WebSocket;

namespace VoteBot.Models;

public class User {
    [Key]
    public uint UserId { get; set; }
    
    [Required]
    public ulong DiscordUserId { get; set; }
    
    [Required]
    public ulong ServerId { get; set; }
    
    // ==== Nav Properties ====

    [ForeignKey(nameof(ServerId))]
    [InverseProperty(nameof(Models.Server.Users))]
    public Server Server { get; set; }

    [InverseProperty(nameof(Models.Option.Advocate))]
    public List<Option> AdvocateFor = new List<Option>();

    [InverseProperty(nameof(Models.Option.Opposition))]
    public List<Option> OppositionFor = new List<Option>();

    [InverseProperty(nameof(Models.Election.Owner))]
    public List<Election> Elections = new List<Election>();

    [InverseProperty(nameof(Models.Vote.User))]
    public List<Vote> Votes = new List<Vote>();
    
    // ==== Util Methods ====

    public SocketGuildUser GetGuildUser(SocketGuild guild) {
        return guild.GetUser(DiscordUserId);
    }

    public SocketGuildUser GetGuildUser(DiscordSocketClient client) {
        return GetGuildUser(Server.GetGuild(client));
    }
}