using FluentAssertions;
using NetArchTest.Rules;

namespace Lexio.Service1.Api.Tests;

/// <summary>
/// NetArchTest architecture fitness functions.
/// These fail CI if layer boundaries are violated — enforcing clean architecture automatically.
/// </summary>
public sealed class ArchitectureTests
{
    [Fact]
    public void Domain_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Lexio.Service1.Domain.DomainAnchor).Assembly)
            .ShouldNot().HaveDependencyOn("Lexio.Service1.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain must not reference Infrastructure");
    }

    [Fact]
    public void Domain_ShouldNot_DependOn_Application()
    {
        var result = Types.InAssembly(typeof(Lexio.Service1.Domain.DomainAnchor).Assembly)
            .ShouldNot().HaveDependencyOn("Lexio.Service1.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Lexio.Service1.Application.DependencyInjection).Assembly)
            .ShouldNot().HaveDependencyOn("Lexio.Service1.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Application must not reference Infrastructure — only abstractions via interfaces");
    }

    [Fact]
    public void Application_ShouldNot_DependOn_Api()
    {
        var result = Types.InAssembly(typeof(Lexio.Service1.Application.DependencyInjection).Assembly)
            .ShouldNot().HaveDependencyOn("Lexio.Service1.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Infrastructure_ShouldNot_DependOn_Api()
    {
        var result = Types.InAssembly(typeof(Lexio.Service1.Infrastructure.DependencyInjection).Assembly)
            .ShouldNot().HaveDependencyOn("Lexio.Service1.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Infrastructure must not reference the Api presentation layer");
    }
}
