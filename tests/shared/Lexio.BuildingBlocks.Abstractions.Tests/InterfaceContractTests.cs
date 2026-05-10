using FluentAssertions;
using Lexio.BuildingBlocks.Abstractions.Auth;
using Lexio.BuildingBlocks.Abstractions.Caching;
using Lexio.BuildingBlocks.Abstractions.Messaging;
using Lexio.BuildingBlocks.Abstractions.Outbox;
using Lexio.BuildingBlocks.Abstractions.Persistence;

namespace Lexio.BuildingBlocks.Abstractions.Tests;

/// <summary>
/// Smoke / contract tests — assert interface shapes are as expected.
/// These lock the public API so accidental breaking changes surface at build time.
/// </summary>
public sealed class InterfaceContractTests
{
    [Fact]
    public void IEventBus_IsAnInterface()
    {
        typeof(IEventBus).IsInterface.Should().BeTrue();
    }

    [Fact]
    public void IIntegrationEvent_IsAnInterface()
    {
        typeof(IIntegrationEvent).IsInterface.Should().BeTrue();
    }

    [Fact]
    public void IIntegrationEvent_HasIdAndOccurredAt()
    {
        var props = typeof(IIntegrationEvent).GetProperties();
        props.Should().Contain(p => p.Name == "Id" && p.PropertyType == typeof(Guid));
        props.Should().Contain(p => p.Name == "OccurredAt" && p.PropertyType == typeof(DateTimeOffset));
    }

    [Fact]
    public void IIntegrationEventHandler_IsGenericInterface()
    {
        typeof(IIntegrationEventHandler<>).IsInterface.Should().BeTrue();
        typeof(IIntegrationEventHandler<>).IsGenericTypeDefinition.Should().BeTrue();
    }

    [Fact]
    public void IOutbox_IsAnInterface_WithAppendAndDispatch()
    {
        var type = typeof(IOutbox);
        type.IsInterface.Should().BeTrue();
        type.GetMethod("AppendAsync").Should().NotBeNull();
        type.GetMethod("DispatchPendingAsync").Should().NotBeNull();
    }

    [Fact]
    public void OutboxMessage_IsRecord_WithExpectedProperties()
    {
        var props = typeof(OutboxMessage).GetProperties();
        props.Should().Contain(p => p.Name == "Id" && p.PropertyType == typeof(Guid));
        props.Should().Contain(p => p.Name == "Type" && p.PropertyType == typeof(string));
        props.Should().Contain(p => p.Name == "Payload" && p.PropertyType == typeof(string));
        props.Should().Contain(p => p.Name == "OccurredAt" && p.PropertyType == typeof(DateTimeOffset));
        props.Should().Contain(p => p.Name == "ProcessedAt" && p.PropertyType == typeof(DateTimeOffset?));
    }

    [Fact]
    public void ILexioCache_IsAnInterface_WithGetSetRemove()
    {
        var type = typeof(ILexioCache);
        type.IsInterface.Should().BeTrue();
        // Generic methods — verify by name
        type.GetMethods().Should().Contain(m => m.Name == "GetAsync");
        type.GetMethods().Should().Contain(m => m.Name == "SetAsync");
        type.GetMethods().Should().Contain(m => m.Name == "RemoveAsync");
    }

    [Fact]
    public void ICurrentUserAccessor_IsAnInterface_WithExpectedMembers()
    {
        var type = typeof(ICurrentUserAccessor);
        type.IsInterface.Should().BeTrue();
        type.GetProperty("UserId").Should().NotBeNull();
        type.GetProperty("Email").Should().NotBeNull();
        type.GetProperty("Roles").Should().NotBeNull();
        type.GetProperty("IsAuthenticated").Should().NotBeNull();
    }

    [Fact]
    public void IUnitOfWork_IsAnInterface_WithSaveChangesAsync()
    {
        var type = typeof(IUnitOfWork);
        type.IsInterface.Should().BeTrue();
        type.GetMethod("SaveChangesAsync").Should().NotBeNull();
    }

    [Fact]
    public void NoAbstractionsAssembly_ReferencesAspNetCore()
    {
        // Architecture invariant: BuildingBlocks.Abstractions must NOT depend on AspNetCore.
        var referencedAssemblies = typeof(IEventBus).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty);

        referencedAssemblies.Should().NotContain(
            name => name.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase),
            because: "Abstractions layer must not depend on Microsoft.AspNetCore.*");
    }
}
