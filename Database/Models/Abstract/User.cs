﻿using AuthServer.Database.Models.Abstract.Interfaces;

namespace AuthServer.Database.Models.Abstract;

public abstract class User : IUser
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string EmailAddress { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}