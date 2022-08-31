using LazyCache;
using Microsoft.Extensions.Hosting.WindowsServices;
using Cast.Provider;
using Cast.Provider.Converter;
using Cast.Provider.MediaInfoProvider;
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

            // TODO: metadata should return an empty version when nothing found
            // TODO: use exports module js
            // TODO: bind properly title name JS
            // TODO: bind seek +- buttons
            // TODO: orderby library
            // TODO: make this shit pretty
            // TODO: bind range form to progress
            // TODO: adjust media overlay

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
            builder.Services.AddSingleton<MediaProvider>();
            builder.Services.AddSingleton<IProviderService>
                (x => new CachedMediaProvider(
                    x.GetRequiredService<ILogger<CachedMediaProvider>>(),
                    x.GetRequiredService<MediaProvider>(),
                    x.GetRequiredService<IAppCache>(),
                    x.GetRequiredService<UserProfile>())
                );

            builder.Host.UseWindowsService();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapRazorPages();

            //app.RunAsync();
            app.Run();
        }
    }
}