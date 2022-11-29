using System.Net.Mail;
using AuthServer.Validators.Interfaces;

namespace AuthServer.Validators.Responses;

public record struct UserInfoValidationResponse : IValidationResponse
{
    public MailAddress MailAddress { get; init; }
}