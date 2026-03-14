using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatarPatar.Application.Services;

internal class ValidationService : IValidationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ValidationService> _logger;
    private readonly Dictionary<Type, bool> _validationSettings = new();

    private bool _isValidationDisabledGlobally = false;

    public ValidationService(IServiceProvider serviceProvider, ILogger<ValidationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void SetGlobalValidation(bool isEnabled)
    {
        _isValidationDisabledGlobally = !isEnabled;
    }

    public void EnableValidation<T>(bool isEnabled) where T : class => _validationSettings[typeof(T)] = isEnabled;

    public void Validate<T>(T dto) where T : class
    {
        if (_isValidationDisabledGlobally || (_validationSettings.TryGetValue(typeof(T), out bool isEnabled) && !isEnabled))
            return;

        var validator = _serviceProvider.GetServices<IValidator<T>>().FirstOrDefault();
        if (validator == null)
        {
            _logger.LogWarning("No validator found for {DtoType}. Skipping validation.", typeof(T).Name);
            return;
        }

        ValidationResult result = validator.Validate(dto);

        if (!result.IsValid) throw new ValidationAppException(result.Errors);
    }

    public async Task ValidateAsync<T>(T dto, CancellationToken cancellationToken = default) where T : class
    {
        if (_isValidationDisabledGlobally || (_validationSettings.TryGetValue(typeof(T), out bool isEnabled) && !isEnabled))
            return;

        var validator = _serviceProvider.GetServices<IValidator<T>>().FirstOrDefault();
        if (validator == null)
        {
            _logger.LogWarning("No validator found for {DtoType}. Skipping validation.", typeof(T).Name);
            return;
        }

        ValidationResult result = await validator.ValidateAsync(dto, cancellationToken);

        if (!result.IsValid) throw new ValidationAppException(result.Errors);
    }

    public void ValidateAll<T>(IEnumerable<T> dtos) where T : class
    {
        foreach (var dto in dtos)
            Validate(dto);
    }

    public async Task ValidateAllAsync<T>(IEnumerable<T> dtos, CancellationToken cancellationToken = default) where T : class
    {
        var validationTasks = dtos.Select(dto => ValidateAsync(dto, cancellationToken));
        await Task.WhenAll(validationTasks);
    }
}
