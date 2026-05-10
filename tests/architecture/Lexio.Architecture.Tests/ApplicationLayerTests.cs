// Use public DependencyInjection classes as assembly anchors — implementation types are internal sealed.
// Fully-qualified typeof() calls avoid IDE0005 unused-using warnings from the global usings.
namespace Lexio.Architecture.Tests;

/// <summary>
/// Architecture invariants for Lexio.BuildingBlocks implementation assemblies.
/// Verifies that infrastructure concerns (messaging, caching, observability) do not
/// bleed into the application/domain boundary.
/// </summary>
public sealed class ApplicationLayerTests
{
    // DependencyInjection is the public surface; use fully-qualified typeof() as the assembly anchor.
    private static readonly Types MessagingTypes =
        Types.InAssembly(typeof(Lexio.BuildingBlocks.Messaging.DependencyInjection).Assembly);

    private static readonly Types CachingTypes =
        Types.InAssembly(typeof(Lexio.BuildingBlocks.Caching.DependencyInjection).Assembly);

    private static readonly Types ObservabilityTypes =
        Types.InAssembly(typeof(Lexio.BuildingBlocks.Observability.DependencyInjection).Assembly);

    [Fact]
    public void Messaging_ShouldNot_DependOn_AspNetCore()
    {
        MessagingTypes
            .That().HaveNameStartingWith("Lexio")
            .ShouldNot().HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult()
            .IsSuccessful
            .Should().BeTrue(because: "BuildingBlocks.Messaging must not depend on AspNetCore");
    }

    [Fact]
    public void Caching_ShouldNot_DependOn_EntityFrameworkCore()
    {
        CachingTypes
            .That().HaveNameStartingWith("Lexio")
            .ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult()
            .IsSuccessful
            .Should().BeTrue(because: "BuildingBlocks.Caching must not depend on EF Core");
    }

    [Fact]
    public void Observability_ShouldNot_DependOn_MassTransit()
    {
        ObservabilityTypes
            .That().HaveNameStartingWith("Lexio")
            .ShouldNot().HaveDependencyOn("MassTransit")
            .GetResult()
            .IsSuccessful
            .Should().BeTrue(because: "BuildingBlocks.Observability must not depend on MassTransit");
    }

    [Fact]
    public void Observability_ShouldNot_DependOn_EntityFrameworkCore()
    {
        ObservabilityTypes
            .That().HaveNameStartingWith("Lexio")
            .ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult()
            .IsSuccessful
            .Should().BeTrue(because: "BuildingBlocks.Observability must not depend on EF Core");
    }
}
