using Microsoft.AspNetCore.Builder;

namespace Cast.ServiceWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureAppConfiguration((context, config) =>
                {
                    //config.AddJsonFile(".json");
                    // config.N
                })
                .ConfigureServices(services =>
                {
                    services.AddHostedService<Worker>();
                })
                .Build();





            host.Run();



            //var webApplicationOptions = new WebApplicationOptions() { ContentRootPath = AppContext.BaseDirectory, Args = args, ApplicationName = System.Diagnostics.Process.GetCurrentProcess().ProcessName };
            //var builder = WebApplication.CreateBuilder(webApplicationOptions);

            //var temp = builder.Host.UseWindowsService();
            //WebApplicationBuilder.Build();

        }
    }
}