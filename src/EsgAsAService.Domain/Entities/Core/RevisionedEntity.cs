using System.ComponentModel.DataAnnotations;

namespace EsgAsAService.Domain.Entities.Core;

public abstract class RevisionedEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Logical identifier for all revisions of the same entity.
    /// </summary>
    [Required]
    public Guid RevisionGroupId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Revision number starting at 1.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Version { get; set; } = 1;

    /// <summary>
    /// If false the revision is superseded by a newer one.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [MaxLength(256)]
    public string? CreatedBy { get; set; }
}

