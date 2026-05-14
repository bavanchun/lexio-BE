using Lexio.Identity.Domain.Primitives;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lexio.Identity.Infrastructure.Persistence.ValueConverters;

public sealed class UserIdConverter : ValueConverter<UserId, Guid>
{
    public UserIdConverter() : base(id => id.Value, value => new UserId(value)) { }
}

public sealed class RoleIdConverter : ValueConverter<RoleId, Guid>
{
    public RoleIdConverter() : base(id => id.Value, value => new RoleId(value)) { }
}

public sealed class RefreshTokenIdConverter : ValueConverter<RefreshTokenId, Guid>
{
    public RefreshTokenIdConverter() : base(id => id.Value, value => new RefreshTokenId(value)) { }
}

public sealed class OAuthConnectionIdConverter : ValueConverter<OAuthConnectionId, Guid>
{
    public OAuthConnectionIdConverter() : base(id => id.Value, value => new OAuthConnectionId(value)) { }
}
