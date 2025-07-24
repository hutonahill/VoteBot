using Discord.WebSocket;
using VoteBot.Models;

namespace VoteBot.VotingEngines;

public class PluralityEngine : IVotingEngine {
    public PluralityEngine(Issue issue) {
        Issue = issue;
    }
    
    public void Tabulate(bool finalize = true) {
        Dictionary<Option, uint> unorderedResults = new Dictionary<Option, uint>();
        uint totalVotes = 0;
        
        foreach (Option option in Issue.Options) {
            foreach (Vote vote in option.Votes) {
                unorderedResults[option] += 1;
                totalVotes++;
            }
        }
        
        Results = unorderedResults
            .OrderByDescending(kvp => kvp.Value)
            .ToList();

        if (finalize == true) {
            foreach (Option option in Issue.Options) {
                option.Results.Add(new Result {
                    Option = option,
                    // In this case Value is the percentage of votes received
                    Value = ((double)unorderedResults[option]/totalVotes)
                });
            }
        }
    }
    
    private List<KeyValuePair<Option, uint>> Results = new List<KeyValuePair<Option, uint>>();
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