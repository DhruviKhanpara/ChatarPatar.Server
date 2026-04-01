namespace ChatarPatar.Common.AppLogging.Model.LogRequest;

public class CommunicationLogRequest
{
    public string TargetSystem { get; set; }
    public DeliveryMethod DeliveryMethod { get; set; }
    public DeliveryStatus DeliveryStatus { get; set; }
    public object DeliveryDetails { get; set; }
    public string? ErrorMessage { get; set; }

    public CommunicationLogRequest(string targetSystem, DeliveryMethod deliveryMethod, DeliveryStatus deliveryStatus, object deliveryDetails)
    {
        TargetSystem = targetSystem;
        DeliveryMethod = deliveryMethod;
        DeliveryStatus = deliveryStatus;
        DeliveryDetails = deliveryDetails;
    }

    public CommunicationLogRequest(string targetSystem, DeliveryMethod deliveryMethod, DeliveryStatus deliveryStatus, object deliveryDetails, string? errorMessage) : this(targetSystem, deliveryMethod, deliveryStatus, deliveryDetails)
    {
        TargetSystem = targetSystem;
        DeliveryMethod = deliveryMethod;
        DeliveryStatus = deliveryStatus;
        DeliveryDetails = deliveryDetails;
        ErrorMessage = errorMessage;
    }
}
