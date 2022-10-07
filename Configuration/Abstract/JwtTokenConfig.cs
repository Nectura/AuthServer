namespace AuthServer.Configuration.Abstract;

public abstract class JwtTokenConfig
{
    public string JwtAudience { get; set; } = "";
    public int TimeoutInMilliseconds { get; set; }
}