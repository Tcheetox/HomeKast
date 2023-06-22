using System.Text.Json;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog.Events;
using Serilog;
using Kast.Provider.Conversions;
using Kast.Provider;
using Kast.Provider.Media;
using Kast.Provider.Cast;
using Kast.Api.Problems;
using Kast.Api.Extensions;
using Microsoft.AspNetCore.Hosting;

// TODO: slow refresh of lib is mandatory!

var version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
var match = Regex.Match(version ?? string.Empty, @"\d");
if (string.IsNullOrWhiteSpace(version) || !match.Success)
    throw new InvalidProgramException($"Version number not properly defined in {nameof(AssemblyInformationalVersionAttribute)}");

var builder = WebApplication.CreateBuilder(args);

var path = Path.Combine(builder.Environment.ContentRootPath, "logs");
var template = "{Timestamp:g} [{Level}]   {Message} {NewLine}{Exception}";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Debug)
    .Enrich.FromLogContext()
#if DEBUG
    .WriteTo.File(Path.Combine(path, $"Debug-.log"), LogEventLevel.Debug, outputTemplate: template, retainedFileCountLimit: 3, rollingInterval: RollingInterval.Day)
#endif
    .WriteTo.File(Path.Combine(path, $"Info-.log"), LogEventLevel.Information, outputTemplate: template, retainedFileCountLimit: 3, rollingInterval: RollingInterval.Day)
    .WriteTo.File(Path.Combine(path, $"Error-.log"), LogEventLevel.Error, outputTemplate: template, retainedFileCountLimit: 3, rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddSerilog(Log.Logger, true);
});

// JSON
var sharedSerializationOptions = new JsonSerializerOptions() 
{ 
    PropertyNameCaseInsensitive = true 
};
builder.Services.AddSingleton(sharedSerializationOptions);

builder.Services.AddSingleton<SettingsProvider>();
builder.WebHost.ConfigureKestrel(options =>
{
    var settingsProvider = options.ApplicationServices.GetRequiredService<SettingsProvider>();
    options.ListenLocalhost(settingsProvider.Application.HttpPort, o => o.UseHttps());
    options.Listen(settingsProvider.Application.Ip, settingsProvider.Application.HttpPort);
});

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsContextExtension.Extend);

builder.Services
    .AddControllers(options => options.Filters.Add(new ProducesAttribute("application/json")))
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
#if DEBUG
        options.AllowInputFormatterExceptionMessages = true;
#endif
    });

//builder.Services.AddSingleton<ProblemDetailsFactory, EmptyDetailsFactory>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc($"v{match.Value[0]}", new OpenApiInfo
    {
        Version = version,
        Title = "HomeKast API",
    })
);

builder.Services.AddSingleton<IMetadataProvider, CachedMetadataProvider>();
builder.Services.AddSingleton<IMediaProvider, CachedMediaProvider>();
builder.Services.AddSingleton<IMediaConverter, MediaConverter>();
builder.Services.AddSingleton<ICastProvider, CastProvider>();
builder.Services.AddSingleton<FileWatcher, FileWatcher>();

var app = builder.Build();

foreach (var service in GetAllServices<IRefreshable>(app.Services))
    _ = service.RefreshAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

static IEnumerable<T> GetAllServices<T>(IServiceProvider provider)
{
    var type = typeof(T);
    var site = typeof(ServiceProvider).GetProperty("CallSiteFactory", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(provider);
    if (site == null)
        yield break;
    if (site.GetType().GetField("_descriptors", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(site) is not ServiceDescriptor[] descriptor)
        yield break;
    foreach (var entry in descriptor.Where(s => s.ServiceType.IsAssignableTo(type)).Select(s => provider.GetRequiredService(s.ServiceType)))
        yield return (T)entry;
}