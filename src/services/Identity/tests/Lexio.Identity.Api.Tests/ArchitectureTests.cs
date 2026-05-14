using FluentAssertions;
using NetArchTest.Rules;

namespace Lexio.Identity.Api.Tests;

/// <summary>
/// NetArchTest architecture fitness functions.
/// These fail CI if layer boundaries are violated — enforcing clean architecture automatically.
/// </summary>
public sealed class ArchitectureTests
{
    [Fact]
    public void Domain_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Lexio.Identity.Domain.DomainAnchor).Assembly)
            .ShouldNot().HaveDependencyOn("Lexio.Identity.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain must not reference Infrastructure");
    }

    [Fact]
    public void Domain_ShouldNot_DependOn_Application()
    {
        var result = Types.InAssembly(typeof(Lexio.Identity.Domain.DomainAnchor).Assembly)
            .ShouldNot().HaveDependencyOn("Lexio.Identity.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Lexio.Identity.Application.DependencyInjection).Assembly)
            .ShouldNot().HaveDependencyOn("Lexio.Identity.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Application must not reference Infrastructure — only abstractions via interfaces");
    }

    [Fact]
    public void Application_ShouldNot_DependOn_Api()
    {
        var result = Types.InAssembly(typeof(Lexio.Identity.Application.DependencyInjection).Assembly)
            .ShouldNot().HaveDependencyOn("Lexio.Identity.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Infrastructure_ShouldNot_DependOn_Api()
    {
        var result = Types.InAssembly(typeof(Lexio.Identity.Infrastructure.DependencyInjection).Assembly)
            .ShouldNot().HaveDependencyOn("Lexio.Identity.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Infrastructure must not reference the Api presentation layer");
    }
}
