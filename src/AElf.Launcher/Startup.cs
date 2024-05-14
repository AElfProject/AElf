using System;
using System.Globalization;
using System.Linq;
using AElf.Blockchains.MainChain;
using AElf.Blockchains.SideChain;
using AElf.Kernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Volo.Abp.Modularity;

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
        switch (chainType)
        {
            case ChainType.SideChain:
                AddApplication<SideChainAElfModule>(services);
                break;
            default:
                AddApplication<MainChainAElfModule>(services);
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

        services.OnRegistred(options =>
        {
            if (options.ImplementationType.IsDefined(typeof(UmpAttribute), true))
            {
                options.Interceptors.TryAdd<UmpInterceptor>();
            }
        });
        
        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .AddSource("AElf")
                    .SetSampler(new AlwaysOnSampler())
                    //.AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    // .AddOtlpExporter(options =>
                    //     {
                    //         options.Endpoint = new Uri("http://192.168.66.22:9090/");
                    //         options.Protocol = OtlpExportProtocol.HttpProtobuf;
                    //     }
                    // )
                    ;
                // builder.AddConsoleExporter();
            })
            .WithMetrics(builder =>
            {
                builder
                    .AddMeter("AElf")
                    //.AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation();

                // builder.AddConsoleExporter();
                builder.AddPrometheusExporter();
            });
    }

    private static void AddApplication<T>(IServiceCollection services) where T : IAbpModule
    {
        services.AddApplication<T>();
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
        app.UseOpenTelemetryPrometheusScrapingEndpoint();

        app.InitializeApplication();
    }
}