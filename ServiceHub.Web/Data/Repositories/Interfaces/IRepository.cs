namespace ServiceHub.Web.Data.Repositories.Interfaces;

/// <summary>
/// Defines generic asynchronous CRUD operations for application entities.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
public interface IRepository<T, TKey> where T : class
{
    Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TKey> InsertAsync(T entity, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default);
}
