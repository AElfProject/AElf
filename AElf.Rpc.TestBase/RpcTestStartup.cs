using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

namespace AElf.Rpc.TestBase
{
    public class RpcTestStartup
    {
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddApplication<TestsRpcAElfModule>(options => { options.UseAutofac(); });
            return services.BuildAutofacServiceProvider();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.InitializeApplication();
            app.Run((async context => { context.ToString(); }));
            app.UseCors(builder =>
                builder.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());
        }
    }
}