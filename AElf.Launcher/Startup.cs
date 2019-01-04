using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

namespace AElf.Launcher
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddApplication<LauncherAElfModule>(options =>
            {
                options.UseAutofac();
            });

            return services.BuildAutofacServiceProvider();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            
            /*
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }*/
            
            

            //app.Run(async (context) => { await context.Response.WriteAsync("Hello World!"); });

            app.InitializeApplication();
        }
    }
}