using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using FluentlyHttpClient.Middleware;
using FluentlyHttpClient;

namespace FluentHttpClient.Extensions.Library.Middleware
{
    public interface IJwtBearerAuthData
    {
        string Token { get; }
        DateTime TokenExpiresAt { get; }
    }

    public class JwtBearerAuthManagerMiddlewareOptions
    {
        public IJwtBearerAuthData JwtBearerAuthData { get; set; }
        public Func<Task<IJwtBearerAuthData>> JwtBearerRefreshTokenProcessing { get; set; }

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

        public JwtBearerAuthManagerMiddleware(
            FluentHttpMiddlewareDelegate next,
            FluentHttpMiddlewareClientContext context,
            JwtBearerAuthManagerMiddlewareOptions options)
        {
            _next = next;
            _options = options;
        }

        public async Task<FluentHttpResponse> Invoke(FluentHttpMiddlewareContext context)
        {
            var request = context.Request;

            FluentHttpResponse response;
            try
            {
                await _mutex.WaitAsync();

                if (string.IsNullOrEmpty(_options.JwtBearerAuthData.Token) ||
                    _options.JwtBearerAuthData.TokenExpiresAt <= DateTime.UtcNow)
                {
                    _options.JwtBearerAuthData = await _options.JwtBearerRefreshTokenProcessing();
                }

                _mutex.Release();

                request.Headers.Add(HeaderTypes.Authorization, $"{AuthSchemeTypes.Bearer} {_options.JwtBearerAuthData.Token}");
                response = await _next(context);
            }
            catch (Exception e)
            {
                _mutex.Release();
                throw e;
            }

            return response;
        }
    }
}
