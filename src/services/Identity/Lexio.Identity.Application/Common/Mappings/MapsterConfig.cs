using Lexio.Identity.Application.Features.Auth.Me;
using Lexio.Identity.Application.Features.Roles.List;
using Lexio.Identity.Domain.Entities;
using Mapster;

namespace Lexio.Identity.Application.Common.Mappings;

public sealed class MapsterConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<User, UserDto>()
            .Map(d => d.Id, s => s.Id.Value)
            .Map(d => d.Email, s => s.Email.Value)
            .Map(d => d.DisplayName, s => s.DisplayName.Value)
            .Map(d => d.RoleId, s => s.RoleId.Value)
            .Map(d => d.Status, s => s.Status.ToString())
            .Map(d => d.IsVerified, s => s.IsVerified)
            .Map(d => d.LastLoginAt, s => s.LastLoginAt)
            .Map(d => d.CreatedAt, s => s.CreatedAt);

        config.NewConfig<Role, RoleDto>()
            .Map(d => d.Id, s => s.Id.Value)
            .Map(d => d.Name, s => s.Name)
            .Map(d => d.Description, s => s.Description)
            .Map(d => d.Permissions, s => s.Permissions);
    }
}
