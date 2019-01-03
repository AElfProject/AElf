using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController.Rpc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

namespace AElf.FullNodeHosting
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddApplication<FullNodeHostingAElfModule>(options =>
            {
                options.UseAutofac();
            });


            var service = services.Where(p => p.ImplementationType == typeof(ChainControllerRpcService)).ToList();
            
            return services.BuildServiceProviderFromFactory();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var service = app.ApplicationServices;
            

            //app.Run(async (context) => { await context.Response.WriteAsync("Hello World!"); });

            app.InitializeApplication();
        }
    }
}