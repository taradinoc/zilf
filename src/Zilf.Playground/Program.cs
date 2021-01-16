using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Zilf.Common;
using Zilf.Playground.Services.Builds;
using Zilf.Playground.Services.Workspaces;
using Zilf.Playground.Services.Repl;
using Zilf.Playground.Services.Templates;
using BlazorWorker.Core;

namespace Zilf.Playground
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.Logging.SetMinimumLevel(LogLevel.Debug);

            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            builder.Services.AddSingleton<JSInterop>();
            builder.Services.AddScoped<TemplateService>();
            builder.Services.AddScoped<WorkspaceService>();
            builder.Services.AddScoped<ReplService>();
            builder.Services.AddScoped<BuildService>();

            builder.Services.AddWorkerFactory();

            await builder.Build().RunAsync();
        }
    }
}
