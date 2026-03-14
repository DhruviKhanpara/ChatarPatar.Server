namespace ChatarPatar.Application.ServiceContracts;

public interface IValidationService
{
    void SetGlobalValidation(bool isEnabled);
    void EnableValidation<T>(bool isEnabled) where T : class;
    void Validate<T>(T dto) where T : class;
    Task ValidateAsync<T>(T dto, CancellationToken cancellationToken = default) where T : class;
    void ValidateAll<T>(IEnumerable<T> dtos) where T : class;
    Task ValidateAllAsync<T>(IEnumerable<T> dtos, CancellationToken cancellationToken = default) where T : class;
}
