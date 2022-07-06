using AntDesign.ProLayout;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using XTC.FMP.LIB.MVCS;

namespace XTC.FMP.APP.Blazor
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            ModuleRouter modelRouter = new ModuleRouter();
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(modelRouter.AssemblyResolve);

            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            Logger logger = new ConsoleLogger();
            Framework framework = new Framework();
            framework.setConfig(new Config());
            framework.setLogger(logger);
            framework.Initialize();
           
            var channel = GrpcChannel.ForAddress("https://localhost:19000/", new GrpcChannelOptions
            {
                HttpHandler = new GrpcWebHandler(new HttpClientHandler())
            });
           
            framework.Setup();
           
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(sp => logger);
            builder.Services.AddScoped(sp => framework);
            builder.Services.AddScoped(sp => channel);
            builder.Services.AddScoped(sp => modelRouter);
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddAntDesign();
            builder.Services.Configure<ProSettings>(builder.Configuration.GetSection("ProSettings"));
           
            await builder.Build().RunAsync();

            framework.Dismantle();
            framework.Release();
        }
    }
}