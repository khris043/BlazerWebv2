using BlazorApp1.Components;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

//  container
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

//  HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Logger.LogInformation("Blazor app started. Blog database will initialize on first use.");

app.Run();
