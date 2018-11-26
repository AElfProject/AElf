using Community.AspNetCore;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.Monitor
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddJsonRpcService<AkkaService>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseJsonRpcService<AkkaService>();
        }
    }
}