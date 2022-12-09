namespace AuthServer.Exceptions;

public class OAuthAccessTokenExchangeException : Exception
{
    public OAuthAccessTokenExchangeException(string message) : base(message)
    {
    }
}