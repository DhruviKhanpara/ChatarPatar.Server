namespace ChatarPatar.Application.DTOs.Message.Receipt;

public class MessageReceiptsSummaryDto
{
    public Guid MessageId { get; set; }

    /// <summary>
    /// False for channels, Direct DMs, and groups that were already over
    /// GroupReceiptThreshold at send time — none of these ever get
    /// MessageReceipt rows, so there's nothing to report. Distinguishes
    /// "no data" from "tracked, but nobody's seen it yet" (which instead
    /// returns IsTracked = true with every Receipts entry unseen).
    /// </summary>
    public bool IsTracked { get; set; }

    public List<MessageReceiptItemDto> Receipts { get; set; } = new();
}
