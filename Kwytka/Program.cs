using Kwytka.Components;
using Kwytka.Authentication;
using Kwytka.Configuration;

using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options => options.MaximumReceiveMessageSize = 10 * 1024 * 1024);
builder.Services.AddAuthentication(AdminBasicAuthenticationDefaults.Scheme)
    .AddScheme<AuthenticationSchemeOptions, AdminBasicAuthenticationHandler>(AdminBasicAuthenticationDefaults.Scheme,
        null);
builder.Services.AddAuthorization();
builder.Services.AddOptions<ConfigurationStorageOptions>()
    .Configure(options => options.ConfigPath = builder.Configuration["ConfigPath"] ?? string.Empty)
    .Validate(options => !string.IsNullOrWhiteSpace(options.ConfigPath), "ConfigPath must be configured.")
    .ValidateOnStart();
builder.Services.AddOptions<TableFormattingOptions>()
    .Configure(options => options.CountColumns = builder.Configuration["CountColumns"] ?? options.CountColumns);
builder.Services.AddSingleton<IConfigurationService, JsonConfigurationService>();

var app = builder.Build();

await app.Services.GetRequiredService<IConfigurationService>().LoadAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/admin") && context.User.Identity?.IsAuthenticated != true)
    {
        await context.ChallengeAsync(AdminBasicAuthenticationDefaults.Scheme);
        return;
    }

    await next();
});

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapGet("/admin/settings.json", async (IConfigurationService configurationService, CancellationToken cancellationToken) =>
{
    var settings = await configurationService.ExportAsync(cancellationToken);
    return Results.File(settings, "application/json", "settings.json");
});
app.MapPost("/admin/settings.json", async (IFormFile? settingsFile, IConfigurationService configurationService,
    CancellationToken cancellationToken) =>
{
    if (settingsFile is null || settingsFile.Length == 0)
    {
        return Results.BadRequest("Select a settings file to import.");
    }

    try
    {
        await using var stream = settingsFile.OpenReadStream();
        await configurationService.ImportAsync(stream, cancellationToken);
        return Results.Redirect("/admin");
    }
    catch (InvalidDataException exception)
    {
        return Results.BadRequest(exception.Message);
    }
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
