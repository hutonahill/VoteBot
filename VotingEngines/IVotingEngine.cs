using Discord.WebSocket;
using VoteBot.Models;

namespace VoteBot.VotingEngines;

public interface IVotingEngine {

    public ref DiscordSocketClient Client { get; }

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