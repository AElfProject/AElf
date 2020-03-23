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
using Volo.Abp.Modularity;

namespace AElf.Launcher
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private const string DefaultCorsPolicyName = "CorsPolicy";

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
                    if (_configuration["CorsOrigins"] != "*")
                    {
                        builder.AllowCredentials();
                    }
                });
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
            app.UseCors(DefaultCorsPolicyName);

            app.InitializeApplication();
        }
    }
}