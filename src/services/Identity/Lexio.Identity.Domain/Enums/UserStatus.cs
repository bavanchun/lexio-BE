namespace Lexio.Identity.Domain.Enums;

/// <summary>Lifecycle states of a <see cref="Entities.User"/>.</summary>
public enum UserStatus
{
    /// <summary>Default state on registration. User can authenticate.</summary>
    Active = 0,

    /// <summary>Admin-imposed lockout. User cannot authenticate; existing tokens rejected.</summary>
    Banned = 1,

    /// <summary>Soft-deleted (account closed). Retained for audit.</summary>
    Deleted = 2,
}
