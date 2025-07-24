using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoteBot.Models;

public class Result {
    [Key]
    public uint ResultId { get; set; }
    
    public uint OptionId { get; set; }
    
    public double Value { get; set; }
    
    // Nav Properties
    
    [ForeignKey(nameof(OptionId))]
    [InverseProperty(nameof(Models.Option.Results))]
    public Option Option { get; set; }
    
    public Issue Issue {
        get { return Option.Issue; }
    }
}