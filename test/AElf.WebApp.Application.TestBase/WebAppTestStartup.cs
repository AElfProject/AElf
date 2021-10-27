using AElf.Kernel.SmartContract;
using Microsoft.AspNetCore.Builder;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS;
using AElf.WebApp.Application.Chain;
using AElf.WebApp.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.WebApp.Application
{
    public class WebAppTestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ITxHub, MockTxHub>();
            services.AddApplication<WebAppTestAElfModule>();
            services.Configure<WebAppOptions>(options => { options.TransactionResultStatusCacheSeconds = 2; });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.InitializeApplication();
            app.UseCors(builder =>
                builder.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());
        }
    }
}