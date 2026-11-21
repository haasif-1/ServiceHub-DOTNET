using System.Data.Common;
using Dapper;
using ServiceHub.Web.Data.Repositories.Interfaces;

namespace ServiceHub.Web.Data.Repositories;

/// <summary>
/// Abstract generic repository implementation using Dapper and MySQL.
/// Uses predefined trusted table and column mappings to ensure safety and prevent SQL injection.
/// </summary>
/// <typeparam name="T">The entity model type.</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
public abstract class DapperGenericRepository<T, TKey> : IRepository<T, TKey> where T : class
{
    protected readonly IMySqlConnectionFactory ConnectionFactory;
    protected readonly string TableName;
    protected readonly string KeyColumn;

    protected DapperGenericRepository(
        IMySqlConnectionFactory connectionFactory,
        string tableName,
        string keyColumn = "Id")
    {
        ConnectionFactory = connectionFactory;
        TableName = tableName;
        KeyColumn = keyColumn;
    }

    /// <summary>
    /// Gets a single entity by its primary key.
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT * FROM `{TableName}` WHERE `{KeyColumn}` = @Id LIMIT 1;";

        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<T>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    /// <summary>
    /// Gets all records from the table.
    /// </summary>
    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT * FROM `{TableName}` ORDER BY `{KeyColumn}` DESC;";

        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var results = await connection.QueryAsync<T>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return results.AsList();
    }

    /// <summary>
    /// Inserts an entity and returns the generated primary key.
    /// Derived repositories provide the parameterized SQL and parameters.
    /// </summary>
    public abstract Task<TKey> InsertAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an entity record.
    /// </summary>
    public abstract Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a record by primary key.
    /// </summary>
    public virtual async Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var sql = $"DELETE FROM `{TableName}` WHERE `{KeyColumn}` = @Id;";

        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return affected == 1;
    }

    /// <summary>
    /// Helper to execute database operations within a managed transaction when needed.
    /// </summary>
    protected async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<DbConnection, DbTransaction, Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await action(connection, transaction);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
