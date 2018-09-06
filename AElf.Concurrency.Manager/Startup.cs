using System;
using Community.AspNetCore;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.Concurrency.Manager
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddJsonRpcService<ActorService>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseJsonRpcService<ActorService>();
        }
    }
}