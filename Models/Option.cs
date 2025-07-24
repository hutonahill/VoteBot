using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoteBot.Models;

public class Option {
    [Key]
    public uint OptionId { get; set; }
    
    [Required]
    [StringLength(120)]
    public required string OptionString { get; set; }
    
    [Display(Name="Advocate Id", Description = "The Id of the user who will advocate for the option")]
    public uint? AdvocateId { get; set; }

    [StringLength(1000)]
    public string? For { get; set; }
    
    [Display(Name="Opposition Id", Description = "The Id of the user who will oppose for the option")]
    public uint? OppositionId { get; set; }

    [StringLength(1000)]
    public string? Against { get; set; }

    [Required]
    public uint IssueId { get; set; }
    
    // ==== Nav Properties ====

    [ForeignKey(nameof(AdvocateId))]
    [InverseProperty(nameof(Models.User.AdvocateFor))]
    public User? Advocate { get; set; }

    [ForeignKey(nameof(OppositionId))]
    [InverseProperty(nameof(Models.User.OppositionFor))]
    public User? Opposition { get; set; }
    
    [ForeignKey(nameof(IssueId))]
    [InverseProperty(nameof(Models.Issue.Options))]
    public required Issue Issue { get; set; }

    [InverseProperty(nameof(Models.Vote.Option))]
    public List<Vote> Votes { get; set; } = new List<Vote>();

    [InverseProperty(nameof(Models.Result.Option))]
    public List<Result> Results { get; set; } = new List<Result>();
}