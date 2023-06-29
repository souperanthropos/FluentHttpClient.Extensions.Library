using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Sample.Server.App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                 .AddJwtBearer(options =>
                 {
                     options.RequireHttpsMetadata = false;
                     options.TokenValidationParameters = new TokenValidationParameters
                     {
                         // укзывает, будет ли валидироваться издатель при валидации токена
                         ValidateIssuer = true,
                         // строка, представляющая издателя
                         ValidIssuer = AuthOptions.ISSUER,

                         // будет ли валидироваться потребитель токена
                         ValidateAudience = true,
                         // установка потребителя токена
                         ValidAudience = AuthOptions.AUDIENCE,
                         // будет ли валидироваться время существования
                         ValidateLifetime = true,

                         // установка ключа безопасности
                         IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
                         // валидация ключа безопасности
                         ValidateIssuerSigningKey = true,
                     };
                 });

            builder.Services.AddControllers();

            var app = builder.Build();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}