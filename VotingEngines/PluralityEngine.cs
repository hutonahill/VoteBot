using Discord.WebSocket;
using VoteBot.Models;

namespace VoteBot.VotingEngines;

public class PluralityEngine : IVotingEngine {
    public PluralityEngine(Issue issue) {
        Issue = issue;
    }
    
    public void Tabulate() {
        Dictionary<Option, int> unorderedResults = new Dictionary<Option, int>();
        foreach (Option option in Issue.Options) {
            foreach (Vote vote in option.Votes) {
                unorderedResults[option] += 1;
            }
        }
        
        Results = unorderedResults
            .OrderByDescending(kvp => kvp.Value)
            .ToList();
    }
    
    private List<KeyValuePair<Option, int>> Results = new List<KeyValuePair<Option, int>>();
    public ref DiscordSocketClient Client => throw new NotImplementedException();

    private Issue Issue { get; set; }

    Issue IVotingEngine.Issue {
        get => Issue;
        set => Issue = value;
    }

    public VotingMethods GetMethod() {
        return VotingMethods.Plurality;
    }
    
    public bool UsesValue() {
        return false;
    }

    private class PluralityVote {
        public User voter { get; set; }
        
        public Option selectedOption { get; set; }
    }
}