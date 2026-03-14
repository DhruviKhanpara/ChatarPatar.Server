namespace ChatarPatar.Infrastructure.ExternalServiceContracts;

public interface IExternalServiceManager
{
    ICloudinaryService CloudinaryService { get; }
}
