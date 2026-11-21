using ServiceHub.Web.Data.Repositories.Interfaces;
using ServiceHub.Web.Models.ViewModels;

namespace ServiceHub.Web.Services;

/// <summary>
/// Domain service coordinating provider services business logic and repository interactions.
/// </summary>
public sealed class ProviderServicesService
{
    private readonly IProviderServiceRepository _repository;

    public ProviderServicesService(IProviderServiceRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<ProviderServiceViewModel>> GetForProviderAsync(
        string providerUserId,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetForProviderAsync(providerUserId, cancellationToken);
    }

    public Task<ProviderServiceViewModel?> GetByIdForProviderAsync(
        long id,
        string providerUserId,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetByIdForProviderAsync(id, providerUserId, cancellationToken);
    }

    public Task<long> CreateAsync(
        string providerUserId,
        ProviderServiceFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        return _repository.CreateForProviderAsync(providerUserId, model, cancellationToken);
    }

    public Task<bool> UpdateForProviderAsync(
        long id,
        string providerUserId,
        ProviderServiceFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        return _repository.UpdateForProviderAsync(id, providerUserId, model, cancellationToken);
    }

    public Task<bool> DeleteForProviderAsync(
        long id,
        string providerUserId,
        CancellationToken cancellationToken = default)
    {
        return _repository.DeleteForProviderAsync(id, providerUserId, cancellationToken);
    }
}
