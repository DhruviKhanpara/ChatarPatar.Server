using ChatarPatar.Infrastructure.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities;

public class MessageAttachment : BaseEntity
{
    #region Table References
    [ForeignKey(nameof(MessageId))]
    public Message Message { get; set; } = null!;
    [ForeignKey(nameof(FileId))]
    public FileEntity File { get; set; } = null!;
    #endregion

    public Guid MessageId { get; set; }
    public Guid FileId { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }
}