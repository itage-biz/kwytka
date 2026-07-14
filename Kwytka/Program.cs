using Kwytka.Components;
using Kwytka.Authentication;
using Kwytka.Configuration;

using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
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

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
