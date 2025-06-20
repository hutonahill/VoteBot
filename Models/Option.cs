using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoteBot.Models;

public class Option {
    [Key]
    public uint OptionId { get; set; }
    
    [Required]
    [StringLength(120)]
    public string OptionString { get; set; }

    public uint AdvocateId { get; set; }

    [StringLength(1000)]
    public string For { get; set; }

    public uint OppositionId { get; set; }

    [StringLength(1000)]
    public string Against { get; set; }

    [Required]
    public uint IssueId { get; set; }
    
    // ==== Nav Properties ====

    [ForeignKey(nameof(AdvocateId))]
    [InverseProperty(nameof(Models.User.AdvocateFor))]
    public User Advocate { get; set; }

    [ForeignKey(nameof(OppositionId))]
    [InverseProperty(nameof(Models.User.OppositionFor))]
    public User Opposition { get; set; }
    
    [ForeignKey(nameof(IssueId))]
    [InverseProperty(nameof(Models.Issue.Options))]
    public Issue Issue { get; set; }

    [InverseProperty(nameof(Models.Vote.Option))]
    public List<Vote> Votes = new List<Vote>();
}