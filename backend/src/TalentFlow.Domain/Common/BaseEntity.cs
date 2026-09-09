using System;

namespace TalentFlow.Domain.Common;

/// <summary>
/// Base entity with common audit fields.
/// All entities inherit from this.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
