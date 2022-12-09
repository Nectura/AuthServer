namespace AuthServer.Database.Models.Abstract.Interfaces;

public interface IUser
{
    Guid Id { get; set; }
    string Name { get; set; }
    string EmailAddress { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}