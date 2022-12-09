namespace AuthServer.Models.UserInfo;

public sealed class GoogleUserInfo : Abstract.UserInfo
{
    public string Nonce { get; set; } = "";
}