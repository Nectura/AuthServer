using System.Text.RegularExpressions;

namespace AuthServer.Validators;

public sealed class PasswordStructureValidator
{
    private readonly bool _shouldHaveAtLeastOneCapitalLetter;
    private readonly bool _shouldHaveAtLeastOneDigit;
    private readonly bool _shouldHaveAtLeastOneNonAlphaNumeric;
    private readonly int _minimumLength;

    public PasswordStructureValidator(
        bool shouldHaveAtLeastOneCapitalLetter = true,
        bool shouldHaveAtLeastOneDigit = true,
        bool shouldHaveAtLeastOneNonAlphaNumeric = true,
        int minimumLength = 8)
    {
        _shouldHaveAtLeastOneCapitalLetter = shouldHaveAtLeastOneCapitalLetter;
        _shouldHaveAtLeastOneDigit = shouldHaveAtLeastOneDigit;
        _shouldHaveAtLeastOneNonAlphaNumeric = shouldHaveAtLeastOneNonAlphaNumeric;
        _minimumLength = minimumLength;
    }

    public bool Validate(string password, out string? validationError)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            validationError = "The password input is empty.";
            return false;
        }

        if (!HasAtLeastOneCapitalLetter(password))
        {
            validationError = "The password needs to have at least 1 capital letter.";
            return false;
        }

        if (!HasAtLeastOneDigit(password))
        {
            validationError = "The password needs to have at least 1 digit.";
            return false;
        }

        if (!HasAtLeastOneSpecialCharacter(password))
        {
            validationError = "The password needs to have at least 1 special character.";
            return false;
        }

        if (!HasTheMinimumAmountOfCharacters(password))
        {
            validationError = $"The password needs to consist of at least {_minimumLength:n0} {(_minimumLength == 1 ? "character" : "characters")}.";
            return false;
        }

        validationError = default;
        return true;
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

    private bool HasTheMinimumAmountOfCharacters(string password)
    {
        return password.Length >= _minimumLength;
    }
}