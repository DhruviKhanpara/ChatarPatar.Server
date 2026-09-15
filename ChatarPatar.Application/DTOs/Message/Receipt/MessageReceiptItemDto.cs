namespace ChatarPatar.Application.DTOs.Message.Receipt;

public class MessageReceiptItemDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = null!;

    public DateTime? DeliveredAt { get; set; }
    public DateTime? SeenAt { get; set; }
}
