using ServiceHub.Web.Models;
using ServiceHub.Web.Models.ViewModels;

namespace ServiceHub.Web.Data.Repositories.Interfaces;

/// <summary>
/// Specialized repository for Provider Service operations, enforcing provider ownership scoping.
/// </summary>
public interface IProviderServiceRepository : IRepository<ProviderService, long>
{
    Task<IReadOnlyList<ProviderServiceViewModel>> GetForProviderAsync(
        string providerUserId,
        CancellationToken cancellationToken = default);

    Task<ProviderServiceViewModel?> GetByIdForProviderAsync(
        long id,
        string providerUserId,
        CancellationToken cancellationToken = default);

    Task<long> CreateForProviderAsync(
        string providerUserId,
        ProviderServiceFormViewModel model,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateForProviderAsync(
        long id,
        string providerUserId,
        ProviderServiceFormViewModel model,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteForProviderAsync(
        long id,
        string providerUserId,
        CancellationToken cancellationToken = default);
}
