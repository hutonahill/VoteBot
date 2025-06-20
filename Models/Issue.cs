using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Discord.WebSocket;

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
    
    [Required]
    public HashSet<ulong> VotingRoleIds = new HashSet<ulong>();

    // ==== Nav Properties ====
    
    [ForeignKey(nameof(ElectionId))]
    [InverseProperty(nameof(Models.Election.Issues))]
    public Election Election { get; set; }

    [InverseProperty(nameof(Models.Option.Issue))]
    public List<Option> Options = new List<Option>();
    
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
    
}

public enum VotingMethods {
    RankedChoice,
    MultiWinnerRankedChoice,
    ApprovalVoting,
    MultiWinnerApproval,
    FirstPastThePost,
    Condorcet,
    Score
}