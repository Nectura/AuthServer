namespace AuthServer.Validators.Exceptions;

public sealed class ValidationException : Exception
{
    public ValidationException(string validationError) : base(validationError) {}
}