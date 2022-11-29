using System.Net.Mail;
using AuthServer.Validators.Exceptions;
using AuthServer.Validators.Interfaces;
using AuthServer.Validators.Requests;
using AuthServer.Validators.Responses;

namespace AuthServer.Validators;

public sealed class UserInfoValidator : IValidator<UserInfoValidationRequest, UserInfoValidationResponse> 
{
    public UserInfoValidationResponse TryValidate(UserInfoValidationRequest validatorRequest)
    {
        if (!MailAddress.TryCreate(validatorRequest.Input, out var mailAddress))
            throw new ValidationException("Invalid email address");
        
        return new UserInfoValidationResponse
        {
            MailAddress = mailAddress
        };
    }
}