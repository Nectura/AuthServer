using AuthServer.Validators.Interfaces;

namespace AuthServer.Validators.Requests;

public record struct PasswordStructureValidationRequest(
    string PasswordInput = "",
    bool ShouldHaveAtLeastOneCapitalLetter = true,
    bool ShouldHaveAtLeastOneDigit = true,
    bool ShouldHaveAtLeastOneNonAlphaNumeric = true,
    int MinimumLength = 8) : IValidationRequest
{
    public string PasswordInput { get; init; } = PasswordInput;
    public bool ShouldHaveAtLeastOneCapitalLetter { get; init; } = ShouldHaveAtLeastOneCapitalLetter;
    public bool ShouldHaveAtLeastOneDigit { get; init; } = ShouldHaveAtLeastOneDigit;
    public bool ShouldHaveAtLeastOneNonAlphaNumeric { get; init; } = ShouldHaveAtLeastOneNonAlphaNumeric;
    public int MinimumLength { get; init; } = MinimumLength;
}