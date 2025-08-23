using FluentlyHttpClient;
using FluentlyHttpClient.Middleware;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FluentHttpClient.Extensions.Library.Middleware
{
    public interface IJwtBearerAuthData
    {
        string Token { get; }
        DateTime TokenExpiresAt { get; }
        string RefreshToken { get; }
    }

    public class JwtBearerAuthManagerMiddlewareOptions
    {
        public IJwtBearerAuthData JwtBearerAuthData { get; }

        public Func<IJwtBearerAuthData, Task<IJwtBearerAuthData>> JwtBearerRefreshTokenProcessing { get; set; }

        public JwtBearerAuthManagerMiddlewareOptions(IJwtBearerAuthData jwtBearerAuthData)
        {
            JwtBearerAuthData = jwtBearerAuthData;
        }
    }

    public class JwtBearerAuthManagerMiddleware : IFluentHttpMiddleware
    {
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1);
        private readonly FluentHttpMiddlewareDelegate _next;
        private readonly JwtBearerAuthManagerMiddlewareOptions _options;

        private IJwtBearerAuthData JwtBearerAuthData { get; set; }

        public JwtBearerAuthManagerMiddleware(
            FluentHttpMiddlewareDelegate next,
            FluentHttpMiddlewareClientContext context,
            JwtBearerAuthManagerMiddlewareOptions options)
        {
            _next = next;
            _options = options;
            JwtBearerAuthData = options.JwtBearerAuthData;
        }

        public async Task<FluentHttpResponse> Invoke(FluentHttpMiddlewareContext context)
        {
            var request = context.Request;

            FluentHttpResponse response;
            bool isMutexReleased = false;

            try
            {
                await _mutex.WaitAsync();

                if (string.IsNullOrEmpty(JwtBearerAuthData?.Token) ||
                    JwtBearerAuthData?.TokenExpiresAt <= DateTime.UtcNow)
                {
                    JwtBearerAuthData = await _options.JwtBearerRefreshTokenProcessing(JwtBearerAuthData);
                }

                _mutex.Release();
                isMutexReleased = true;

                request.Headers.Add(HeaderTypes.Authorization, $"{AuthSchemeTypes.Bearer} {JwtBearerAuthData.Token}");
                response = await _next(context);
            }
            catch (Exception e)
            {
                if(!isMutexReleased) _mutex.Release();
                throw e;
            }

            return response;
        }
    }
}
