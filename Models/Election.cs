﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using Discord.WebSocket;

namespace VoteBot.Models;

public class Election {
    [Key]
    public uint ElectionId { get; set; }
    
    [StringLength(50)]
    public string? Name { get; set; }
    
    [Display(Name ="Publication Date", Description ="The date the election is made public.")]
    public DateTime? PublicationDate { get; set; }

    [Required]
    [Display(Name="Start Time", Description ="The time when voting starts.")]
    public DateTime StartTime { get; set; }

    [Required]
    [Display(Name="End Time", Description ="The time when voting ends.")]
    public DateTime EndTime { get; set; }

    [Required]
    public uint OwnerId { get; set; }
    
    [Required]
    public ulong ServerId { get; set; }

    [Required]
    public bool PublicResults { get; set; }

    [Required]
    public bool AnonymousVotes { get; set; }
    
    // ==== Nav Properties ====

    [InverseProperty(nameof(Models.Issue.Election))]
    public List<Issue> Issues { get; set; } = new List<Issue>();

    [ForeignKey(nameof(OwnerId))]
    [InverseProperty(nameof(Models.User.Elections))]
    public User Owner { get; set; }

    [ForeignKey(nameof(ServerId))]
    [InverseProperty(nameof(Models.Server.Elections))]
    public Server Server { get; set; }
    
    // ==== Util Methods ====

    public override string ToString() {
        if (Name == null) {
            return $"Election #{ElectionId}";
        }
        else {
            return Name;
        }
    }
}