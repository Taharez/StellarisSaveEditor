using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StellarisSaveEditor.BlazorClient.Helpers;

namespace StellarisSaveEditor.BlazorClient
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            builder.Services.AddTransient(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            builder.Services.AddBlazoredLocalStorage(config =>
            {
                config.JsonSerializerOptions.Converters.Add(new DictionaryTKeyInt32TValueConverter());
                config.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
            });

            await builder.Build().RunAsync();
        }
    }
}
