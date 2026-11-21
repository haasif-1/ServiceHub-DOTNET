using Dapper;
using ServiceHub.Web.Data.Repositories.Interfaces;
using ServiceHub.Web.Models;
using ServiceHub.Web.Models.ViewModels;

namespace ServiceHub.Web.Data.Repositories;

/// <summary>
/// Concrete repository for provider services.
/// Inherits the generic Dapper repository and implements provider ownership-scoped operations.
/// </summary>
public sealed class ProviderServiceRepository : DapperGenericRepository<ProviderService, long>, IProviderServiceRepository
{
    private const string Table = "ProviderServices";

    public ProviderServiceRepository(IMySqlConnectionFactory connectionFactory)
        : base(connectionFactory, Table, "Id")
    {
    }

    public override async Task<long> InsertAsync(ProviderService entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO ProviderServices
                (ProviderUserId, ServiceName, Description, Price, EstimatedDurationMinutes,
                 IsActive, CreatedAtUtc, UpdatedAtUtc)
            VALUES
                (@ProviderUserId, @ServiceName, @Description, @Price, @EstimatedDurationMinutes,
                 @IsActive, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));
            SELECT LAST_INSERT_ID();
            """;

        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                sql,
                new
                {
                    entity.ProviderUserId,
                    entity.ServiceName,
                    entity.Description,
                    entity.Price,
                    entity.EstimatedDurationMinutes,
                    entity.IsActive
                },
                cancellationToken: cancellationToken));
    }

    public override async Task<bool> UpdateAsync(ProviderService entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE ProviderServices
            SET ServiceName = @ServiceName,
                Description = @Description,
                Price = @Price,
                EstimatedDurationMinutes = @EstimatedDurationMinutes,
                IsActive = @IsActive,
                UpdatedAtUtc = UTC_TIMESTAMP(6)
            WHERE Id = @Id;
            """;

        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    entity.Id,
                    entity.ServiceName,
                    entity.Description,
                    entity.Price,
                    entity.EstimatedDurationMinutes,
                    entity.IsActive
                },
                cancellationToken: cancellationToken));

        return affected == 1;
    }

    public async Task<IReadOnlyList<ProviderServiceViewModel>> GetForProviderAsync(
        string providerUserId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, ServiceName, Description, Price, EstimatedDurationMinutes,
                   IsActive, CreatedAtUtc, UpdatedAtUtc
            FROM ProviderServices
            WHERE ProviderUserId = @ProviderUserId
            ORDER BY CreatedAtUtc DESC, Id DESC;
            """;

        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var services = await connection.QueryAsync<ProviderServiceViewModel>(
            new CommandDefinition(sql, new { ProviderUserId = providerUserId }, cancellationToken: cancellationToken));

        return services.AsList();
    }

    public async Task<ProviderServiceViewModel?> GetByIdForProviderAsync(
        long id,
        string providerUserId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, ServiceName, Description, Price, EstimatedDurationMinutes,
                   IsActive, CreatedAtUtc, UpdatedAtUtc
            FROM ProviderServices
            WHERE Id = @Id AND ProviderUserId = @ProviderUserId
            LIMIT 1;
            """;

        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<ProviderServiceViewModel>(
            new CommandDefinition(
                sql,
                new { Id = id, ProviderUserId = providerUserId },
                cancellationToken: cancellationToken));
    }

    public async Task<long> CreateForProviderAsync(
        string providerUserId,
        ProviderServiceFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO ProviderServices
                (ProviderUserId, ServiceName, Description, Price, EstimatedDurationMinutes,
                 IsActive, CreatedAtUtc, UpdatedAtUtc)
            VALUES
                (@ProviderUserId, @ServiceName, @Description, @Price, @EstimatedDurationMinutes,
                 @IsActive, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));
            SELECT LAST_INSERT_ID();
            """;

        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                sql,
                new
                {
                    ProviderUserId = providerUserId,
                    model.ServiceName,
                    model.Description,
                    model.Price,
                    model.EstimatedDurationMinutes,
                    model.IsActive
                },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateForProviderAsync(
        long id,
        string providerUserId,
        ProviderServiceFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE ProviderServices
            SET ServiceName = @ServiceName,
                Description = @Description,
                Price = @Price,
                EstimatedDurationMinutes = @EstimatedDurationMinutes,
                IsActive = @IsActive,
                UpdatedAtUtc = UTC_TIMESTAMP(6)
            WHERE Id = @Id AND ProviderUserId = @ProviderUserId;
            """;

        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    Id = id,
                    ProviderUserId = providerUserId,
                    model.ServiceName,
                    model.Description,
                    model.Price,
                    model.EstimatedDurationMinutes,
                    model.IsActive
                },
                cancellationToken: cancellationToken));

        return affected == 1;
    }

    public async Task<bool> DeleteForProviderAsync(
        long id,
        string providerUserId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            DELETE FROM ProviderServices
            WHERE Id = @Id AND ProviderUserId = @ProviderUserId;
            """;

        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { Id = id, ProviderUserId = providerUserId },
                cancellationToken: cancellationToken));

        return affected == 1;
    }
}
