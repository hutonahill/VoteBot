using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Discord.WebSocket;
using VoteBot.VotingEngines;

namespace VoteBot.Models;

public class Issue {
    [Key]
    public uint IssueId { get; set; }

    [Required]
    public uint ElectionId { get; set; }

    [Required]
    [StringLength(60)]
    public string Name { get; set; }

    [Required]
    public VotingMethods VotingMethod { get; set; }
    
    //TODO: EF Core does not support List<primitive types>. change this.
    [Required]
    public HashSet<ulong> VotingRoleIds { get; set; } = new HashSet<ulong>();

    // ==== Nav Properties ====
    
    [ForeignKey(nameof(ElectionId))]
    [InverseProperty(nameof(Models.Election.Issues))]
    public Election Election { get; set; }

    [InverseProperty(nameof(Models.Option.Issue))]
    public List<Option> Options { get; set; } = new List<Option>();
    
    public List<Result>? Results => Options
        .SelectMany(o => o.Results)
        .ToList();
    
    // ==== Util Methods ====

    public IVotingEngine GetVotingEngine() {
        //TODO: make this work.

        return null;
    }
    
    public HashSet<SocketRole> GetRoles(SocketGuild guild) {
        HashSet<SocketRole> roles = new HashSet<SocketRole>();

        foreach (ulong roleId in VotingRoleIds) {
            if (guild.Roles.Count(r => r.Id == roleId) > 0) {
                roles.Add(guild.GetRole(roleId));
            }
            else {
                //TODO: handle the role not existing.
            }
        }

        return roles;
    }

    public HashSet<SocketRole> GetRoles(DiscordSocketClient client) {
        return GetRoles(Election.Server.GetGuild(client));
    }

    public HashSet<SocketGuildUser> GetVoters(SocketGuild guild) {
        HashSet<SocketRole> roles = GetRoles(guild);

        HashSet<SocketGuildUser> voters = new HashSet<SocketGuildUser>();

        foreach (SocketRole role in roles) {
            voters.UnionWith(role.Members);
        }
        
        return voters;
    }

    public HashSet<SocketGuildUser> GetVoters(DiscordSocketClient client) {
        return GetVoters(Election.Server.GetGuild(client));
    }

    /* Might not need this, but it's here if I do.
     public HashSet<Vote> GetVotes() {
        HashSet<Vote> voters = new HashSet<Vote>();

        foreach (Option option in Options) {
            voters.UnionWith(option.Votes);
        }

        return voters;
    }*/

}

public enum VotingMethods {
    Plurality,
    RankedChoice,
    MultiWinnerRankedChoice,
    ApprovalVoting,
    MultiWinnerApproval,
    FirstPastThePost,
    Condorcet,
    Score
}