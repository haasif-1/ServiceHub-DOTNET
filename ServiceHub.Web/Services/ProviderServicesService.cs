using Dapper;
using ServiceHub.Web.Data;
using ServiceHub.Web.Models.ViewModels;

namespace ServiceHub.Web.Services;

public sealed class ProviderServicesService
{
    private readonly IMySqlConnectionFactory _connectionFactory;

    public ProviderServicesService(IMySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<ProviderServiceViewModel>> GetForProviderAsync(
        string providerUserId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT Id, ServiceName, Description, Price, EstimatedDurationMinutes,
                   IsActive, CreatedAtUtc, UpdatedAtUtc
            FROM ProviderServices
            WHERE ProviderUserId = @ProviderUserId
            ORDER BY CreatedAtUtc DESC, Id DESC;
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var services = await connection.QueryAsync<ProviderServiceViewModel>(
            new CommandDefinition(sql, new { ProviderUserId = providerUserId }, cancellationToken: cancellationToken));

        return services.AsList();
    }

    public async Task<ProviderServiceViewModel?> GetByIdForProviderAsync(
        long id,
        string providerUserId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT Id, ServiceName, Description, Price, EstimatedDurationMinutes,
                   IsActive, CreatedAtUtc, UpdatedAtUtc
            FROM ProviderServices
            WHERE Id = @Id AND ProviderUserId = @ProviderUserId
            LIMIT 1;
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ProviderServiceViewModel>(
            new CommandDefinition(
                sql,
                new { Id = id, ProviderUserId = providerUserId },
                cancellationToken: cancellationToken));
    }

    public async Task<long> CreateAsync(
        string providerUserId,
        ProviderServiceFormViewModel model,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO ProviderServices
                (ProviderUserId, ServiceName, Description, Price, EstimatedDurationMinutes,
                 IsActive, CreatedAtUtc, UpdatedAtUtc)
            VALUES
                (@ProviderUserId, @ServiceName, @Description, @Price, @EstimatedDurationMinutes,
                 @IsActive, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(
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

        return await connection.ExecuteScalarAsync<long>(
            new CommandDefinition("SELECT LAST_INSERT_ID();", cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateForProviderAsync(
        long id,
        string providerUserId,
        ProviderServiceFormViewModel model,
        CancellationToken cancellationToken)
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

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var affectedRows = await connection.ExecuteAsync(
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

        return affectedRows == 1;
    }

    public async Task<bool> DeleteForProviderAsync(
        long id,
        string providerUserId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE FROM ProviderServices
            WHERE Id = @Id AND ProviderUserId = @ProviderUserId;
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var affectedRows = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { Id = id, ProviderUserId = providerUserId },
                cancellationToken: cancellationToken));

        return affectedRows == 1;
    }
}
