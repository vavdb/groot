using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Groot.Web;
using Groot.UI.Audio;
using Groot.Web.Audio;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<WebCuePlayer>();
builder.Services.AddScoped<ICuePlayer>(sp => sp.GetRequiredService<WebCuePlayer>());

await builder.Build().RunAsync();
