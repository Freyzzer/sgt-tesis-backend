using SGT.Tesis.Application.Common.Abstractions;

namespace SGT.Tesis.Infrastructure.Identity;

internal sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
