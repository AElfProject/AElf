using System;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

namespace AElf.WebApp.Application
{
    public class WebAppTestStartup
    {
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddApplication<WebAppTestAElfModule>(options => { options.UseAutofac(); });
            return services.BuildAutofacServiceProvider();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.InitializeApplication();
            app.UseCors(builder =>
                builder.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());
        }
    }
}