namespace AuthServer.Models.UserInfo;

public sealed class TwitchUserInfo : Abstract.UserInfo
{
    public string BroadcasterType { get; set; } = "";
}