// Use public types as assembly anchors — implementation types (CurrentUserAccessor,
// LexioExceptionHandlingMiddleware) are internal sealed.
namespace Lexio.Architecture.Tests;

/// <summary>
/// Architecture invariants for Lexio infrastructure-tier building blocks:
/// Auth, Persistence, and Web.
/// </summary>
public sealed class InfrastructureLayerTests
{
    // LexioDbContextBase is public abstract — correct anchor for Persistence assembly.
    private static readonly Types PersistenceTypes =
        Types.InAssembly(typeof(Lexio.BuildingBlocks.Persistence.LexioDbContextBase).Assembly);

    // DependencyInjection is the public surface for Auth and Web assemblies.
    private static readonly Types AuthTypes =
        Types.InAssembly(typeof(Lexio.BuildingBlocks.Auth.DependencyInjection).Assembly);

    private static readonly Types WebTypes =
        Types.InAssembly(typeof(Lexio.BuildingBlocks.Web.DependencyInjection).Assembly);

    [Fact]
    public void Persistence_ShouldNot_DependOn_MassTransit()
    {
        PersistenceTypes
            .That().HaveNameStartingWith("Lexio")
            .ShouldNot().HaveDependencyOn("MassTransit")
            .GetResult()
            .IsSuccessful
            .Should().BeTrue(because: "BuildingBlocks.Persistence must not depend on MassTransit");
    }

    [Fact]
    public void Persistence_ShouldNot_DependOn_AspNetCore()
    {
        PersistenceTypes
            .That().HaveNameStartingWith("Lexio")
            .ShouldNot().HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult()
            .IsSuccessful
            .Should().BeTrue(because: "BuildingBlocks.Persistence must not depend on AspNetCore");
    }

    [Fact]
    public void Auth_ShouldNot_DependOn_EntityFrameworkCore()
    {
        AuthTypes
            .That().HaveNameStartingWith("Lexio")
            .ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult()
            .IsSuccessful
            .Should().BeTrue(because: "BuildingBlocks.Auth must not depend on EF Core");
    }

    [Fact]
    public void Web_ShouldNot_DependOn_EntityFrameworkCore()
    {
        WebTypes
            .That().HaveNameStartingWith("Lexio")
            .ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult()
            .IsSuccessful
            .Should().BeTrue(because: "BuildingBlocks.Web must not depend on EF Core");
    }

    [Fact]
    public void Web_ShouldNot_DependOn_MassTransit()
    {
        WebTypes
            .That().HaveNameStartingWith("Lexio")
            .ShouldNot().HaveDependencyOn("MassTransit")
            .GetResult()
            .IsSuccessful
            .Should().BeTrue(because: "BuildingBlocks.Web must not depend on MassTransit");
    }
}
