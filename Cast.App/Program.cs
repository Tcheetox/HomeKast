using Cast.Provider;
using Cast.Provider.Conversions;
using Cast.Provider.Meta;
using Cast.SharedModels;
using Cast.SharedModels.User;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Net.Http.Headers;

namespace Cast.App
{
    // TODO: centralize and rework Media status management
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
            builder.Services.AddLogging(logBuilder =>
            {
                var path = Path.Combine(builder.Environment.ContentRootPath, "logs");
                var template = "{Timestamp:g} [{Level}]   {Message} {NewLine}{Exception}";
                logBuilder.AddFile(Path.Combine(path, "Info.log"), LogLevel.Information, outputTemplate: template, retainedFileCountLimit: 10);
                logBuilder.AddFile(Path.Combine(path, "Warning.log"), LogLevel.Warning, outputTemplate: template);
            });

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
            builder.Services.AddSingleton<IMetadataProvider, CachedMetadataProvider>();
            builder.Services.AddSingleton<IMediaProvider, CachedMediaProvider>();
            builder.Services.AddSingleton<FileWatcher>();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
                app.UseExceptionHandler("/Error");

            app.Services
                .GetRequiredService<FileWatcher>()
                .Start();

            // Serve specific local directory
            var staticFilesDirectory = app
                .Services
                .GetRequiredService<UserProfile>()
                .Application
                .StaticFilesDirectory;
            Directory.CreateDirectory(staticFilesDirectory);

            void staticCaching(StaticFileResponseContext ctx) =>
                ctx.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
                {
                    Public = true,
                    MaxAge = TimeSpan.FromDays(30),
                };

            // Serve wwwroot
            app.UseStaticFiles(new StaticFileOptions() 
            { 
                OnPrepareResponse = staticCaching 
            }); 
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(staticFilesDirectory),
                RequestPath = new PathString('/' + Helper.STATIC_FILES_DIRECTORY),
                OnPrepareResponse = staticCaching
            });

            app.MapRazorPages();

            app.Run();
        }
    }
}