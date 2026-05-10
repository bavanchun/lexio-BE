using Lexio.BuildingBlocks.Abstractions.Messaging;

namespace Lexio.Architecture.Tests;

/// <summary>
/// Architecture invariants for all Lexio.BuildingBlocks.* assemblies.
/// </summary>
public sealed class BuildingBlocksTests
{
    private static readonly Types AbstractionsTypes =
        Types.InAssembly(typeof(IEventBus).Assembly);

    [Fact]
    public void Abstractions_ShouldNot_DependOn_AspNetCore()
    {
        AbstractionsTypes
            .That().HaveNameStartingWith("Lexio")
            .ShouldNot().HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult()
            .IsSuccessful
            .Should().BeTrue(because: "BuildingBlocks.Abstractions must not depend on AspNetCore");
    }

    [Fact]
    public void Abstractions_ShouldNot_DependOn_EntityFrameworkCore()
    {
        AbstractionsTypes
            .That().HaveNameStartingWith("Lexio")
            .ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult()
            .IsSuccessful
            .Should().BeTrue(because: "BuildingBlocks.Abstractions must not depend on EF Core");
    }

    [Fact]
    public void Abstractions_ShouldNot_DependOn_MassTransit()
    {
        AbstractionsTypes
            .That().HaveNameStartingWith("Lexio")
            .ShouldNot().HaveDependencyOn("MassTransit")
            .GetResult()
            .IsSuccessful
            .Should().BeTrue(because: "BuildingBlocks.Abstractions must not depend on MassTransit");
    }
}
