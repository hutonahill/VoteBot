using System.Text.Json;

namespace VoteBot;

public static class ValidateSecrets {
    public static void ValidateAndApply(this WebApplicationBuilder builder, string secretPath) {
        string path = Path.Combine(AppContext.BaseDirectory, "secrets.json");

        if (!File.Exists(path)) {
            throw new FileNotFoundException("Missing secrets.json");
        }
        else {
            Console.WriteLine("secrets.json exists.");
            Console.WriteLine("Verifying secrets.json.");
        }

        JsonDocument? doc;
        try {
            using FileStream stream = File.OpenRead(secretPath);
            doc = JsonDocument.Parse(stream);
        } catch (JsonException e) {
            throw new JsonException("Invalid JSON format in secrets.json.", innerException: e);
        }
        
        Console.WriteLine("Parsed JSON data from secrets.json.");
        
        JsonElement root = doc.RootElement;

        if (!root.TryGetProperty("Discord", out JsonElement discordSection) || 
            discordSection.ValueKind != JsonValueKind.Object) {
            throw new InvalidDataException("The section 'Discord' is Missing or invalid from secrets.json.");
        }

        if (!discordSection.TryGetProperty("Token", out JsonElement tokenElement) ||
            tokenElement.ValueKind != JsonValueKind.String) {
            throw new InvalidDataException("The section 'Token' is Missing or invalid from the 'Discord' section of secrets.json.");
        }

        Console.WriteLine("secrets.json validated successfully.");
        
        // Add the validated secrets.json file to the configuration system:
        // - optional: false → throw an error if the file is missing
        // - reloadOnChange: true → automatically reload configuration if the file changes while the app is running
        builder.Configuration.AddJsonFile(path, optional: false);
        
    }
}