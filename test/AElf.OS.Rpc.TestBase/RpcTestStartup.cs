using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

namespace AElf.OS.Rpc
{
    public class RpcTestStartup
    {
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddApplication<TestBaseRpcAElfModule>(options => { options.UseAutofac(); });
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