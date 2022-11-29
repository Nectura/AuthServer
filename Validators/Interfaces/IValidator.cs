namespace AuthServer.Validators.Interfaces;

public interface IValidator<in T, out TT>
    where T : IValidationRequest
    where TT : IValidationResponse
{
    TT TryValidate(T request);
}