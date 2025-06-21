using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Discord.WebSocket;

namespace VoteBot.Models;

public class Server {
    [Key]
    public ulong ServerId { get; set; }
    
    //TODO: EF Core does not support List<primitive types>. change this.
    public List<ulong> RolesAllowedToCreate { get; set; } = new List<ulong>();

    // ==== Nav Properties

    [InverseProperty(nameof(Models.User.Server))]
    public List<User> Users = new List<User>();

    [InverseProperty(nameof(Models.Election.Server))]
    public List<Election> Elections = new List<Election>();

    public SocketGuild GetGuild(DiscordSocketClient client) {
        return client.GetGuild(ServerId);
    }
}