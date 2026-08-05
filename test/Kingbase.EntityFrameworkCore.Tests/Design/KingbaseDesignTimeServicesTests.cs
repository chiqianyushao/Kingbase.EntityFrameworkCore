using System.Reflection;
using Kingbase.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Kingbase.EntityFrameworkCore.Tests.Design;

public sealed class KingbaseDesignTimeServicesTests
{
    [Fact]
    public void Assembly_exposes_design_time_services()
    {
        var attribute = typeof(KingbaseDesignTimeServices).Assembly
            .GetCustomAttribute<DesignTimeProviderServicesAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(typeof(KingbaseDesignTimeServices).FullName, attribute.TypeName);
    }

    [Fact]
    public void Design_time_services_register_the_provider()
    {
        var services = new ServiceCollection();

        new KingbaseDesignTimeServices().ConfigureDesignTimeServices(services);
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IDatabaseProvider>());
    }
}
