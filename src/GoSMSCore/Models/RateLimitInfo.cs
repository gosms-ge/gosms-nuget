using System.Linq;
using System.Net.Http.Headers;

namespace GoSMSCore.Models;

public class RateLimitInfo
{
    public int? Limit { get; set; }
    public int? Remaining { get; set; }
    public int? RetryAfter { get; set; }

    public static RateLimitInfo? FromHeaders(HttpResponseHeaders headers)
    {
        int? limit = null;
        int? remaining = null;
        int? retryAfter = null;

        if (headers.TryGetValues("X-RateLimit-Limit", out var limitValues))
        {
            if (int.TryParse(limitValues.FirstOrDefault(), out var l))
                limit = l;
        }

        if (headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues))
        {
            if (int.TryParse(remainingValues.FirstOrDefault(), out var r))
                remaining = r;
        }

        if (headers.TryGetValues("Retry-After", out var retryValues))
        {
            if (int.TryParse(retryValues.FirstOrDefault(), out var ra))
                retryAfter = ra;
        }

        if (limit == null && remaining == null && retryAfter == null)
            return null;

        return new RateLimitInfo
        {
            Limit = limit,
            Remaining = remaining,
            RetryAfter = retryAfter
        };
    }
}
