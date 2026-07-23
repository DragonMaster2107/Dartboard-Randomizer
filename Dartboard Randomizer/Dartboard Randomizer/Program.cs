using Dartboard_Randomizer;
using Dartboard_Randomizer.Core.ViewModels;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();

// App-weiter Spielzustand (Scoped == Singleton in WASM: lebt die ganze App-Session).
builder.Services.AddSingleton<GameController>();

await builder.Build().RunAsync();
