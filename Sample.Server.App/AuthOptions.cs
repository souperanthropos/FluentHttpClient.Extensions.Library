using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Sample.Server.App
{
    public class AuthOptions
    {
        public const string ISSUER = "WeatherForecast";
        public const string AUDIENCE = "WeatherForecastClient";
        const string KEY = "supersecret_secretkey";
        public const int LIFETIME = 1;
        public const int RefreshTokenExpiryTime = 7;
        public static SymmetricSecurityKey GetSymmetricSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(KEY));
        }
    }
}
