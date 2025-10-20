using Newtonsoft.Json;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace FluentHttpClient.Extensions.Library.Middleware
{
    public abstract class AuthenticationTokensBase : IAuthenticationTokens
    {
        [JsonProperty("id_token")]
        public string IdToken { get; set; }

        [JsonProperty("access_token")]
        public string Token { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        public abstract string Scheme { get; }

        public virtual string Scope { get; set; } = null;

        public virtual object AuthOptions { get; set; } = null;

        private AuthenticatedUserInfo _user;
        [JsonIgnore]
        public AuthenticatedUserInfo User
        {
            get
            {
                if (_user == null)
                {
                    _user = GetUserFromToken();
                }
                return _user;
            }
        }

        [JsonIgnore]
        private DateTime? _tokenExpiresAt;
        public DateTime? TokenExpiresAt
        {
            get
            {
                if (_tokenExpiresAt == null)
                {
                    _tokenExpiresAt = GetTokenExpiry();
                }
                return _tokenExpiresAt;
            }
        }

        private DateTime? GetTokenExpiry()
        {
            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(Token))
                return null;

            var jwtToken = handler.ReadJwtToken(Token);
            var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp");

            if (expClaim == null || !long.TryParse(expClaim.Value, out var expUnix))
                return null;

            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
            return expiresAt;
        }

        protected virtual AuthenticatedUserInfo GetUserFromToken()
        {
            if (string.IsNullOrEmpty(Token))
            {
                return new AuthenticatedUserInfo();
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(Token);

                var displayName = jwtToken.Claims.FirstOrDefault(c => c.Type == "full_name")?.Value;
                if (string.IsNullOrEmpty(displayName))
                {
                    displayName = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.FamilyName)?.Value + " " + jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.UniqueName)?.Value;
                }

                return new AuthenticatedUserInfo
                {
                    DisplayName = displayName,
                    Email = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value
                };
            }
            catch
            {
                return new AuthenticatedUserInfo();
            }
        }
    }
}
