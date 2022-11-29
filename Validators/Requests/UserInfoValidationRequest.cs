using AuthServer.Validators.Interfaces;

namespace AuthServer.Validators.Requests;

public record struct UserInfoValidationRequest : IValidationRequest
{
    public string Input { get; init; }
}