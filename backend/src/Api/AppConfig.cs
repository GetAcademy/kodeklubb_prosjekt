namespace Api;

/// <summary>
/// Static configuration helper for accessing application configuration without dependency injection
/// </summary>
public static class AppConfig
{
    private static IConfiguration? _configuration;

    public static IConfiguration Configuration
    {
        get => _configuration ?? throw new InvalidOperationException("AppConfig not initialized. Call Initialize() first.");
        private set => _configuration = value;
    }

    public static string ConnectionString { get; set; } = string.Empty;

    public static void Initialize(IConfiguration configuration)
    {
        Configuration = configuration;
    }
}
