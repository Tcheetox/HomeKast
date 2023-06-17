using System.Text.Json;
using System.Reflection;
using Serilog.Events;
using Serilog;
using Kast.Provider.Conversions;
using Kast.Provider;
using Kast.Provider.Media;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Diagnostics;

// TODO: use application port from settings

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

builder.Services
    .AddControllers(options => options.Filters.Add(new ProducesAttribute("application/json")))
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
#if DEBUG
        options.AllowInputFormatterExceptionMessages = true;
#endif
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc($"v{match.Value[0]}", new OpenApiInfo
    {
        Version = version,
        Title = "HomeKast API",
    })
);

builder.Services.AddSingleton(new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
builder.Services.AddSingleton<SettingsProvider>();
builder.Services.AddSingleton<IMetadataProvider, CachedMetadataProvider>();
builder.Services.AddSingleton<IMediaProvider, CachedMediaProvider>();
builder.Services.AddSingleton<IMediaConverter, MediaConverter>();
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

app.UseHttpsRedirection();

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