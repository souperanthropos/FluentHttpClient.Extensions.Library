using FluentHttpClient.Extensions.Library.Middleware;
using FluentlyHttpClient;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace Sample.Client.App
{
    internal class Program
    {
        public static IServiceCollection CreateContainer()
            => new ServiceCollection()
                .AddFluentlyHttpClient()
                .AddLogging();
        static async Task Main(string[] args)
        {
            var container = CreateContainer();
            var serviceProvider = container.BuildServiceProvider();
            try
            {
                var fluentHttpClientFactory = serviceProvider.GetRequiredService<IFluentHttpClientFactory>();

                fluentHttpClientFactory.CreateBuilder(identifier: "gettoken")
                    .WithMessageHandler(new HttpClientHandler { ServerCertificateCustomValidationCallback = delegate { return true; } })
                    .WithBaseUrl("https://localhost:7161/api/token")
                    .WithHeader("user-agent", "Sample.Client.App")
                    .Register();
                fluentHttpClientFactory.CreateBuilder(identifier: "refreshtoken")
                    .WithMessageHandler(new HttpClientHandler { ServerCertificateCustomValidationCallback = delegate { return true; } })
                    .WithBaseUrl("https://localhost:7161/api/refresh-token")
                    .WithHeader("user-agent", "Sample.Client.App")
                    .Register();

                var getTokenHttpClient = fluentHttpClientFactory.Get("gettoken");
                var refreshTokenHttpClient = fluentHttpClientFactory.Get("refreshtoken");

                var tokenResponse =
                    await getTokenHttpClient.CreateRequest()
                        .AsPost()
                        .WithBody(new
                        {
                            Login = "weather1",
                            Password = "12345"
                        })
                        .ReturnAsResponse<JwtBearerAuthData>();

                fluentHttpClientFactory.CreateBuilder(identifier: "getdata")
                  .WithMessageHandler(new HttpClientHandler { ServerCertificateCustomValidationCallback = delegate { return true; } })
                  .WithBaseUrl("https://localhost:7161")
                  .WithHeader("user-agent", "Sample.Client.App")
                  .UseMiddleware<JwtBearerAuthManagerMiddleware>(new JwtBearerAuthManagerMiddlewareOptions(tokenResponse.Data)
                  {
                      JwtBearerRefreshTokenProcessing = async () =>
                      {
                          var refreshTokenResponse =
                          await refreshTokenHttpClient.CreateRequest()
                              .AsPost()
                              .WithBody(new
                              {
                                  AccessToken = tokenResponse.Data.Token,
                                  tokenResponse.Data.RefreshToken
                              })
                              .ReturnAsResponse<JwtBearerAuthData>();
                          return refreshTokenResponse.Data;
                      }
                  })
                  .Register();

                var fluentHttpClient = fluentHttpClientFactory.Get("getdata");

                await GetDataAsync(fluentHttpClient, 1);

                Console.WriteLine();
                Console.WriteLine($"Waiting 60 sec");
                Console.WriteLine();

                await Task.Delay(60000);

                await GetDataAsync(fluentHttpClient, 2);
            }
            catch(Exception e)
            {
                Console.WriteLine($"Exception: {e}");
            }
        }

        private static async Task GetDataAsync(IFluentHttpClient fluentHttpClient, int step)
        {
            Console.WriteLine($"Step: {step}");

            var dataResponse =
              await fluentHttpClient.CreateRequest("/api/v1/weatherforecast")
                .AsGet()
                .WithHeader("user-agent", "Sample.Client.App")
                .ReturnAsResponse();

            if (dataResponse.IsSuccessStatusCode)
            {
                var content = await dataResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Success: {content}");
            }
            else
            {
                Console.WriteLine($"Response error for step {step}");
            }
        }
    }
}