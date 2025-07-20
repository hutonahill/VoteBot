using System.Data.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VoteBot.Models;

namespace VoteBot.Data;

public class VoteContext(DbContextOptions<VoteContext> options) : DbContext(options) {
    public DbSet<Election> Elections { get; set; }
    public DbSet<Issue> Issues { get; set; }
    public DbSet<Option> Options { get; set; }
    public DbSet<Server> Servers { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Vote> Votes { get; set; }
}