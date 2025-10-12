using FluentlyHttpClient;
using FluentlyHttpClient.Middleware;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FluentHttpClient.Extensions.Library.Middleware
{
    public class AuthenticatedUserInfo
    {
        public string UserId { get; set; }          // GUID, SID, или другой уникальный идентификатор
        public string Username { get; set; }        // Имя пользователя или email
        public string DisplayName { get; set; }     // Отображаемое имя
        public string Email { get; set; }           // Email, если доступен
        public List<string> Roles { get; set; }     // Роли или claims

        public AuthenticatedUserInfo()
        {
            Roles = new List<string>();
        }
    }

    public interface IAuthenticationTokens
    {
        string Token { get; }
        DateTime TokenExpiresAt { get; }
        string RefreshToken { get; }
        string Scheme { get; }

        /// <summary>
        /// Специфичные настройки для схемы (например, ADFS, JWT)
        /// </summary>
        object AuthOptions { get; }

        /// <summary>
        /// Информация о пользователе, полученная из токена или провайдера.
        /// </summary>
        AuthenticatedUserInfo User { get; }
    }

    public class TokenAuthMiddlewareOptions
    {
        /// <summary>
        /// Текущий набор токенов (JWT, ADFS и т.д.)
        /// </summary>
        public IAuthenticationTokens TokenSet { get; }

        /// <summary>
        /// Делегат, отвечающий за обновление токенов.
        /// Получает текущий токен и возвращает обновлённый.
        /// </summary>
        public Func<IAuthenticationTokens, Task<IAuthenticationTokens>> RefreshTokensAsync { get; set; }

        public TokenAuthMiddlewareOptions(IAuthenticationTokens tokenSet)
        {
            TokenSet = tokenSet ?? throw new ArgumentNullException(nameof(tokenSet));
        }

        /// <summary>
        /// Попытка привести AuthOptions к нужному типу.
        /// </summary>
        public TOptions GetOptions<TOptions>() where TOptions : class
        {
            return TokenSet.AuthOptions as TOptions;
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
