using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Silo.Launcher;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        AddApplication<AElfSiloLauncherModule>(services);
    }

    private static void AddApplication<T>(IServiceCollection services) where T : IAbpModule
    {
        services.AddApplication<T>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var cultureInfo = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        app.InitializeApplication();
    }
}