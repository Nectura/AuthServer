namespace AuthServer.Database.Models.Abstract.Interfaces;

public interface IUser
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string EmailAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}