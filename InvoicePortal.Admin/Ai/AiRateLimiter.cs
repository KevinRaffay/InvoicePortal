using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace InvoicePortal.Admin.Ai;

/// <summary>
/// Per-circuit fixed window limiter (default 10 requests / minute), the analogue of the reference API's
/// express-rate-limit on its AI endpoints. Registered scoped, so each Blazor circuit gets its own window.
/// </summary>
public sealed class AiRateLimiter(IOptions<AiOptions> options) : IDisposable
{
    private readonly FixedWindowRateLimiter limiter = new(new FixedWindowRateLimiterOptions
    {
        PermitLimit = options.Value.RateLimitPerMinute,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0,
        AutoReplenishment = true,
    });

    /// <summary>Consumes one permit or throws <see cref="AiRateLimitException"/>.</summary>
    public void Acquire()
    {
        using var lease = limiter.AttemptAcquire();
        if (!lease.IsAcquired)
        {
            throw new AiRateLimitException();
        }
    }

    public void Dispose() => limiter.Dispose();
}
