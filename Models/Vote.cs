using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.SignalR;

namespace VoteBot.Models;

public class Vote {
    [Key]
    public uint VoteId { get; set; }
    
    [Required]
    public uint UserId { get; set; }

    [Required]
    public uint OptionId { get; set; }
    
    public int? Value { get; set; }
    
    [Required]
    public DateTime PlacedAt { get; set; }
    
    // ==== Nav Properties ====

    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(Models.User.Votes))]
    public User User { get; set; }

    [ForeignKey(nameof(OptionId))]
    [InverseProperty(nameof(Models.Option.Votes))]
    public Option Option { get; set; }
}