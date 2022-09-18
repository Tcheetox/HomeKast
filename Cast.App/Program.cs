using Cast.Provider;
using Cast.Provider.Converter;
using Cast.Provider.Meta;
using Cast.SharedModels.User;
using Microsoft.Extensions.Hosting.WindowsServices;

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
            builder.Services.AddSingleton<IMetadataProvider, MetadataProvider>();
            builder.Services.AddSingleton<IMediaProvider, CachedMediaProvider>();
            builder.Services.AddSingleton<FileWatcher>();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
                app.UseExceptionHandler("/Error");

            app.Services
                .GetRequiredService<FileWatcher>()
                .Start();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapRazorPages();

            app.Run();
        }
    }
}