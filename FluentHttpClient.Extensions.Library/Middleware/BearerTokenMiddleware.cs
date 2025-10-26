using FluentlyHttpClient;
using FluentlyHttpClient.Middleware;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FluentHttpClient.Extensions.Library.Middleware
{
    public enum AuthScheme
    {
        Jwt,
        Adfs
    }

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
        string IdToken { get; }
        string Token { get; }
        int ExpiresIn { get; }
        string RefreshToken { get; }
        AuthScheme Scheme { get; }
        string Scope { get; }

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
        /// Делегат, отвечающий за обновление токенов.
        /// Получает текущий токен и возвращает обновлённый.
        /// </summary>
        public Func<Task<string>> GetTokenAsync { get; set; }
    }

    public class BearerTokenMiddleware : IFluentHttpMiddleware
    {
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1);
        private readonly FluentHttpMiddlewareDelegate _next;
        private readonly TokenAuthMiddlewareOptions _options;

        public BearerTokenMiddleware(
            FluentHttpMiddlewareDelegate next,
            FluentHttpMiddlewareClientContext context,
            TokenAuthMiddlewareOptions options)
        {
            _next = next;
            _options = options;
        }

        public async Task<FluentHttpResponse> Invoke(FluentHttpMiddlewareContext context)
        {
            var request = context.Request;

            FluentHttpResponse response;
            bool isMutexReleased = false;
            var token = string.Empty;

            try
            {
                await _mutex.WaitAsync();

                token = await _options.GetTokenAsync();

                _mutex.Release();
                isMutexReleased = true;

                request.Headers.Add(HeaderTypes.Authorization, $"{AuthSchemeTypes.Bearer} {token}");
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
