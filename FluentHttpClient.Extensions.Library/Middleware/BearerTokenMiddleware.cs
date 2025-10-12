using FluentlyHttpClient;
using FluentlyHttpClient.Middleware;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FluentHttpClient.Extensions.Library.Middleware
{
    public interface IAuthenticationTokens
    {
        string Token { get; }
        DateTime TokenExpiresAt { get; }
        string RefreshToken { get; }
    }

    public class TokenAuthMiddlewareOptions
    {
        public IAuthenticationTokens TokenSet { get; }

        public Func<IAuthenticationTokens, Task<IAuthenticationTokens>> RefreshTokensAsync { get; set; }

        public TokenAuthMiddlewareOptions(IAuthenticationTokens tokenSet)
        {
            TokenSet = tokenSet;
        }
    }

    public class BearerTokenMiddleware : IFluentHttpMiddleware
    {
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1);
        private readonly FluentHttpMiddlewareDelegate _next;
        private readonly TokenAuthMiddlewareOptions _options;

        private IAuthenticationTokens TokenSet { get; set; }

        public BearerTokenMiddleware(
            FluentHttpMiddlewareDelegate next,
            FluentHttpMiddlewareClientContext context,
            TokenAuthMiddlewareOptions options)
        {
            _next = next;
            _options = options;
            TokenSet = options.TokenSet;
        }

        public async Task<FluentHttpResponse> Invoke(FluentHttpMiddlewareContext context)
        {
            var request = context.Request;

            FluentHttpResponse response;
            bool isMutexReleased = false;

            try
            {
                await _mutex.WaitAsync();

                if (string.IsNullOrEmpty(TokenSet?.Token) ||
                    TokenSet?.TokenExpiresAt <= DateTime.UtcNow)
                {
                    TokenSet = await _options.RefreshTokensAsync(TokenSet);
                }

                _mutex.Release();
                isMutexReleased = true;

                request.Headers.Add(HeaderTypes.Authorization, $"{AuthSchemeTypes.Bearer} {TokenSet.Token}");
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
