using AElf.Kernel.SmartContract;
using Microsoft.AspNetCore.Builder;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.WebApp.Application
{
    public class WebAppTestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ITxHub, MockTxHub>();
            services.AddApplication<WebAppTestAElfModule>();
            services.Configure<ContractOptions>(options => { options.IsTxExecutionTimeoutEnabled = false; });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.InitializeApplication();
            app.UseCors(builder =>
                builder.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());
        }
    }
}