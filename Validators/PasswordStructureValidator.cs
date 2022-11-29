using System.Text.RegularExpressions;
using AuthServer.Validators.Exceptions;
using AuthServer.Validators.Interfaces;
using AuthServer.Validators.Requests;
using AuthServer.Validators.Responses;

namespace AuthServer.Validators;

public sealed class PasswordStructureValidator : IValidator<PasswordStructureValidationRequest, PasswordStructureValidationResponse>
{
    public PasswordStructureValidationResponse TryValidate(PasswordStructureValidationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PasswordInput))
            throw new ValidationException("The password input is empty.");
        
        if (request.ShouldHaveAtLeastOneCapitalLetter && !HasAtLeastOneCapitalLetter(request.PasswordInput))
            throw new ValidationException("The password needs to have at least 1 capital letter.");

        if (request.ShouldHaveAtLeastOneDigit && !HasAtLeastOneDigit(request.PasswordInput))
            throw new ValidationException("The password needs to have at least 1 digit.");

        if (request.ShouldHaveAtLeastOneNonAlphaNumeric && !HasAtLeastOneSpecialCharacter(request.PasswordInput))
            throw new ValidationException("The password needs to have at least 1 special character.");

        if (!HasTheMinimumAmountOfCharacters(request.PasswordInput, request.MinimumLength))
            throw new ValidationException($"The password needs to consist of at least {request.MinimumLength:n0} {(request.MinimumLength == 1 ? "character" : "characters")}.");

        return new PasswordStructureValidationResponse();
    }
    
    private static bool HasAtLeastOneCapitalLetter(string password)
    {
        return Regex.IsMatch(password, "(?=.*[A-Z])");
    }

    private static bool HasAtLeastOneDigit(string password)
    {
        return Regex.IsMatch(password, "(?=.*\\d)");
    }

    private static bool HasAtLeastOneSpecialCharacter(string password)
    {
        return Regex.IsMatch(password, "(?=.*[-+_!@#$%^&*.,?])");
    }

    private static bool HasTheMinimumAmountOfCharacters(string password, int minimumLength)
    {
        return password.Length >= minimumLength;
    }
}