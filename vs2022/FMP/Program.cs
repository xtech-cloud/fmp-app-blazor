using AntDesign.ProLayout;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using XTC.FMP.LIB.MVCS;

namespace XTC.FMP.APP.Blazor
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.Services.Configure<ProSettings>(builder.Configuration.GetSection("ProSettings"));

            ModuleRouter modelRouter = new ModuleRouter();
            RuntimeScalingManager scalingManager = new RuntimeScalingManager();
            scalingManager.SetInternalHttpClient(new Uri(builder.HostEnvironment.BaseAddress));
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(modelRouter.AssemblyResolve);

            Logger logger = new ConsoleLogger();
            Framework framework = new Framework();
            framework.setConfig(new Config());
            framework.setLogger(logger);
            framework.Initialize();

            framework.Setup();

            builder.RootComponents.Add<App>("#app");


            builder.Services.AddScoped(sp => logger);
            builder.Services.AddScoped(sp => framework);
            builder.Services.AddScoped(sp => modelRouter);
            builder.Services.AddScoped(sp => scalingManager);
            builder.Services.AddAntDesign();

            await builder.Build().RunAsync();

            framework.Dismantle();
            framework.Release();
        }
    }
}