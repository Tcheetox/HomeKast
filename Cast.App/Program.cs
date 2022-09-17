using Microsoft.Extensions.Hosting.WindowsServices;
using Cast.Provider;
using Cast.Provider.Converter;
using Cast.Provider.Meta;
using Cast.SharedModels.User;

namespace Cast.App
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var options = new WebApplicationOptions
            {
                Args = args,
                ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
            };
            var builder = WebApplication.CreateBuilder(options);
            builder.Services.AddCors(options =>
                options.AddDefaultPolicy(cors =>
                    cors.WithOrigins("*")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowAnyOrigin()
                ));

            // TODO: bind properly title name JS
            // TODO: bind seek +- buttons
            // TODO: make this shit pretty
            // TODO: replace the ugly SVG omagad

            builder.Services.AddRazorPages();
            builder.Services.AddLazyCache();
            builder.Services.AddSingleton<UserProfile>();
            builder.WebHost.ConfigureKestrel(options =>
            {
                var profile = options.ApplicationServices.GetRequiredService<UserProfile>();
                options.ListenLocalhost(profile.Application.Port);
                options.Listen(profile.Application.IP, profile.Application.Port);
            });

            builder.Services.AddSingleton<IMediaConverter, MediaConverter>();
            builder.Services.AddSingleton<IMetadataProvider, MetadataProvider>();
            builder.Services.AddSingleton<IMediaProvider, MediaProvider>();
            builder.Services.AddSingleton<FileWatcher>();

            // builder.Host.UseWindowsService();

            var app = builder.Build();
            app.Services
                .GetRequiredService<FileWatcher>()
                .Start();

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseCors();

            app.UseAuthorization();

            app.MapRazorPages();

            //app.RunAsync();
            app.Run();
        }
    }
}