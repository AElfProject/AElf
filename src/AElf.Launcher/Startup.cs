using System;
using AElf.Blockchains.MainChain;
using AElf.Blockchains.SideChain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Launcher
{
    public enum ChainType
    {
        Main,
        Side
    }
    
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {

            var chainType = _configuration.GetValue<ChainType>("ChainType");

            switch (chainType)
            {
                case ChainType.Side:
                    AddApplication<SideChainAElfModule>(services);
                    break;
                default:
                    AddApplication<MainChainAElfModule>(services);
                    break;
            }

            return services.BuildAutofacServiceProvider();
        }
        
        private static void AddApplication<T>(IServiceCollection services) where T: IAbpModule
        {
            services.AddApplication<T>(options => { options.UseAutofac(); });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseCors(builder => builder
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
            );

            app.InitializeApplication();
        }
    }
}