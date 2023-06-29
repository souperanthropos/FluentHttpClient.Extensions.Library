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
                         // ��������, ����� �� �������������� �������� ��� ��������� ������
                         ValidateIssuer = true,
                         // ������, �������������� ��������
                         ValidIssuer = AuthOptions.ISSUER,

                         // ����� �� �������������� ����������� ������
                         ValidateAudience = true,
                         // ��������� ����������� ������
                         ValidAudience = AuthOptions.AUDIENCE,
                         // ����� �� �������������� ����� �������������
                         ValidateLifetime = true,

                         // ��������� ����� ������������
                         IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
                         // ��������� ����� ������������
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