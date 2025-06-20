using Discord;
using Discord.WebSocket;
using VoteBot.Models;

namespace VoteBot;

public interface IVotingEngine {

    public DiscordSocketClient Client { get; protected set; }

    public Issue Issue { get; protected set; }

    public VotingMethods GetMethod();

    public bool UsesValue();

    public HashSet<SocketRole> GetVotingRoles() {
        return Issue.GetRoles(Client);
    }

    public HashSet<SocketGuildUser> GetVoters() {
        return Issue.GetVoters(Client);
    }

}