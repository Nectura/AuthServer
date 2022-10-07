namespace AuthServer.Configuration;

public sealed class CorsConfig
{
    public string PolicyName { get; set; } = "";
    public string[] Origins { get; set; } = Array.Empty<string>();
    public string[] ExposedHeaders { get; set; } = Array.Empty<string>();
}