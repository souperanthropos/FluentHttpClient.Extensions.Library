using System.Text.Json.Serialization;

namespace Sample.Server.App.Models
{
    public class User
    {
        public string Login { get; set; }
        public string Password { get; set; }
        [JsonIgnore]
        public string RefreshToken { get; set; }
        [JsonIgnore]
        public DateTime RefreshTokenExpiryTime { get; set; }
    }
}
