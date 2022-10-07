using System.Net.Mail;

namespace AuthServer.Validators;

public sealed class UserInfoValidator
{
    public bool TryValidateEmailAddress(string input, out MailAddress? mailAddress)
    {
        return MailAddress.TryCreate(input, out mailAddress);
    }
}