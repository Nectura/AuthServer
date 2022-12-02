using AuthServer.Validators.Requests;
using AuthServer.Validators.Responses;

namespace AuthServer.Validators.Interfaces;

public interface IPasswordStructureValidator : IValidator<PasswordStructureValidationRequest, PasswordStructureValidationResponse>
{
}