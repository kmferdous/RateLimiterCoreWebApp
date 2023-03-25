using CoreWebApp.Attributes;
using CoreWebApp.Interfaces;
using System.Net;

namespace CoreWebApp.Middlewares
{

    public class CustomRateLimitConfiguration //: RateLimitConfiguration
    {
        private readonly RequestDelegate _next;
        private readonly IRateLimiter _rateLimiter;

        public CustomRateLimitConfiguration(RequestDelegate next, IRateLimiter rateLimiter)
        {
            _next = next;
            _rateLimiter = rateLimiter;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            var limitProp = endpoint?.Metadata.GetMetadata<LimitRequests>();
            if (limitProp is not null)
            {
                var isRequestConformed = await _rateLimiter.ConformRequestAsync(limitProp.TimeWindow, limitProp.MaxRequests, GenerateClientKey(context));
                if (isRequestConformed == false)
                {
                    Console.WriteLine("TooManyRequests");
                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    context.Response.Headers["Retry-After"] = "1";
                    context.Response.ContentType = "application/json";
                    return;
                }
            }

            await _next(context);
        }


        private string GenerateClientKey(HttpContext context)
        {
            
            return string.Format("RateLimit_ip_{0}", context.Connection.RemoteIpAddress); //context.User.Identity.Name
            //return $"{context.Request.Path}_{context.Connection.RemoteIpAddress}";
        }

    }
}