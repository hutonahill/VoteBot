using System.Data.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VoteBot.Models;

namespace VoteBot.Data;

public class VoteContext : DbContext {
    public DbSet<Election> Elections;
    public DbSet<Issue> Issues;
    public DbSet<Option> Options;
    public DbSet<Server> Servers;
    public DbSet<User> Users;
    public DbSet<Vote> Votes;
    
    
    
    
}