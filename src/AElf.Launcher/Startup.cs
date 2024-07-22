using System;
using System.Globalization;
using System.IO;
using System.Linq;
using AElf.Blockchains.MainChain;
using AElf.Blockchains.SideChain;
using AElf.Kernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity.PlugIns;

namespace AElf.Launcher;

public class Startup
{
    private const string DefaultCorsPolicyName = "CorsPolicy";
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        var chainType = _configuration.GetValue("ChainType", ChainType.MainChain);
        var pluginSourcesFolder = _configuration.GetValue("PluginSourcesFolder", Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "aelf", "plugins"));
        Action<AbpApplicationCreationOptions> optionsAction = options =>
        {
            if (Directory.Exists(pluginSourcesFolder))
            {
                options.PlugInSources.AddFolder(pluginSourcesFolder);
            }
        };
        switch (chainType)
        {
            case ChainType.SideChain:
                services.AddApplication<SideChainAElfModule>(optionsAction);
                break;
            case ChainType.MainChain:
                services.AddApplication<MainChainAElfModule>(optionsAction);
                break;
        }

        services.AddCors(options =>
        {
            options.AddPolicy(DefaultCorsPolicyName, builder =>
            {
                builder
                    .WithOrigins(_configuration["CorsOrigins"]
                        .Split(",", StringSplitOptions.RemoveEmptyEntries)
                        .Select(o => o.RemovePostFix("/"))
                        .ToArray()
                    )
                    .WithAbpExposedHeaders()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                if (_configuration["CorsOrigins"] != "*") builder.AllowCredentials();
            });
        });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    // ReSharper disable once UnusedMember.Global
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var cultureInfo = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseCors(DefaultCorsPolicyName);

        app.InitializeApplication();
    }
}