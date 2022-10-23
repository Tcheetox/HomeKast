using Cast.Provider;
using Cast.Provider.Conversions;
using Cast.Provider.Meta;
using Cast.SharedModels.User;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Hosting.WindowsServices;

namespace Cast.App
{
    public static class Program
    {
        // TODO: l'empafure de Bulbi veut changer les images.. omagod kek....
        // TODO: baby readme?
        // TODO: smoother transition in lib using JSON instead
        // TODO: smaller device CSS

        public static void Main(string[] args)
        {
            var options = new WebApplicationOptions
            {
                Args = args,
                ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
            };

            var builder = WebApplication.CreateBuilder(options);
            builder.Services.AddLogging(logBuilder =>
            {
                var path = Path.Combine(builder.Environment.ContentRootPath, "logs");
                var template = "{Timestamp:g} [{Level}]   {Message} {NewLine}{Exception}";
                logBuilder.AddFile(Path.Combine(path, "Info.log"), LogLevel.Information, outputTemplate: template, retainedFileCountLimit: 3);
                logBuilder.AddFile(Path.Combine(path, "Warning.log"), LogLevel.Warning, outputTemplate: template, retainedFileCountLimit: 3);
            });

            builder.Services.AddRazorPages();
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<UserProfile>();
            builder.WebHost.ConfigureKestrel(options =>
            {
                var profile = options.ApplicationServices.GetRequiredService<UserProfile>();
                options.ListenLocalhost(profile.Application.Port);
                options.Listen(profile.Application.IP, profile.Application.Port);
            });

            builder.Services.AddSingleton<IMediaConverter, MediaConverter>();
            builder.Services.AddSingleton<IMetadataProvider, CachedMetadataProvider>();
            builder.Services.AddSingleton<IMediaProvider, CachedMediaProvider>();
            builder.Services.AddSingleton<FileWatcher>();

            builder.Host.UseWindowsService();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
                app.UseExceptionHandler("/Error");

            app.Services
                .GetRequiredService<FileWatcher>()
                .Start();

            // Serve wwwroot
            app.UseStaticFiles(new StaticFileOptions()
            {
#if !DEBUG
                OnPrepareResponse = ctx =>
                    ctx.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
                    {
                        Public = true,
                        MaxAge = TimeSpan.FromDays(30),
                    }
#endif
            });

            app.MapRazorPages();

            app.Run();
        }
    }
}