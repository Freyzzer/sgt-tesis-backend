using SGT.Tesis.Application.Common.Abstractions;

namespace SGT.Tesis.API.Security;

internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId => Guid.TryParse(accessor.HttpContext?.User.FindFirst("sub")?.Value, out var id) ? id : null;

    public string? Rol => accessor.HttpContext?.User.FindFirst("role")?.Value;
}
