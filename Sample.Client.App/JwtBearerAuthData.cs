using FluentHttpClient.Extensions.Library.Middleware;
using Newtonsoft.Json;

namespace Sample.Client.App
{
    public class JwtBearerAuthData : IJwtBearerAuthData
    {
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
        [JsonProperty("access_token")]
        public string Token { get; set; }
        [JsonProperty("expiresAt")]
        public DateTime TokenExpiresAt { get; set; }
    }
}
