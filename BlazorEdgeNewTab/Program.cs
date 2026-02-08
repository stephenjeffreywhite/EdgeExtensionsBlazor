using System;
using System.Net.Http;
using System.Threading.Tasks;
using BlazorEdgeNewTab.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorEdgeNewTab;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.UseBrowserExtension(browserExtension =>
        {
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");
        });
        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
        builder.Services.AddSingleton<SettingsService>(); //Add as singleton since settings should be consistent across the app.
        builder.Services.AddSingleton<NewTabService>(); //Add as singleton since there is only one new tab page instance, and we want to preserve state.
        await builder.Build().RunAsync();
    }
}