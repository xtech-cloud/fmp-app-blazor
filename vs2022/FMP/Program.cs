using AntDesign.ProLayout;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using XTC.FMP.LIB.MVCS;
using XTC.FMP.MOD.StartKit.LIB.MVCS;

namespace XTC.FMP.APP.Blazor
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            var framework = new Framework();
            framework.setConfig(new Config());
            framework.setLogger(new ConsoleLogger());
            framework.Initialize();

           
            var channel = GrpcChannel.ForAddress("https://localhost:19000/", new GrpcChannelOptions
            {
                HttpHandler = new GrpcWebHandler(new HttpClientHandler())
            });

            var entry = new Entry();
            var options = new Options();
            options.channel = channel;
            entry.Inject(framework, options);
            entry.Register();
            framework.Setup();

           
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(sp => entry);
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddAntDesign();
            builder.Services.Configure<ProSettings>(builder.Configuration.GetSection("ProSettings"));

            await builder.Build().RunAsync();

            framework.Dismantle();
            entry.Cancel();
            framework.Release();
        }
    }
}