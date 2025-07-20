using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using VoteBot.Data;
using VoteBot.DiscordBot;

namespace VoteBot;

public class Program {
    
    public DiscordSocketClient Client { get; set; }
    
    private const string secretsFilePath = "secrets.json";
    
    public static void Main(string[] args) {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        
        builder.ValidateAndApply(secretsFilePath);

        // Add services to the container.
        builder.Services.AddRazorPages();

        builder.Services.AddDbContext<VoteContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString(nameof(VoteContext)))
        );
        
        builder.Services.AddHostedService<Bot>();

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment()) {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapRazorPages()
            .WithStaticAssets();

        app.Run();
    }
}