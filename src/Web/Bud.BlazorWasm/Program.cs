using Bud.BlazorWasm;
using Bud.BlazorWasm.Api;
using Bud.BlazorWasm.Infrastructure.Http;
using Bud.BlazorWasm.Services;
using Bud.BlazorWasm.State;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var configuredBaseUrl = builder.Configuration["Api:BaseUrl"];
var apiBaseUrl = string.IsNullOrWhiteSpace(configuredBaseUrl)
    ? builder.HostEnvironment.BaseAddress
    : configuredBaseUrl;

builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<OrganizationContext>();
builder.Services.AddScoped<TenantDelegatingHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<TenantDelegatingHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler) { BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute) };
});
builder.Services.AddScoped<ApiClient>();
builder.Services.AddSingleton<ToastService>();
builder.Services.AddScoped<UiOperationService>();

await builder.Build().RunAsync();
