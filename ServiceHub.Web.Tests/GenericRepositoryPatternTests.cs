using Microsoft.Extensions.Configuration;
using ServiceHub.Web.Data;
using ServiceHub.Web.Data.Repositories;
using ServiceHub.Web.Data.Repositories.Interfaces;
using ServiceHub.Web.Models;
using ServiceHub.Web.Services;

namespace ServiceHub.Web.Tests;

public class GenericRepositoryPatternTests
{
    [Fact]
    public void ProviderServiceRepository_implements_generic_repository_interface()
    {
        var isGenericRepo = typeof(IRepository<ProviderService, long>).IsAssignableFrom(typeof(ProviderServiceRepository));
        Assert.True(isGenericRepo);

        var isSpecializedRepo = typeof(IProviderServiceRepository).IsAssignableFrom(typeof(ProviderServiceRepository));
        Assert.True(isSpecializedRepo);
    }

    [Fact]
    public void ProviderServiceRepository_and_service_instantiate_cleanly()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Port=3306;Database=servicehub_db;User=root;Password=secret;"
        }).Build();

        var connectionFactory = new MySqlConnectionFactory(config);
        var repository = new ProviderServiceRepository(connectionFactory);
        var service = new ProviderServicesService(repository);

        Assert.NotNull(repository);
        Assert.NotNull(service);
    }
}
