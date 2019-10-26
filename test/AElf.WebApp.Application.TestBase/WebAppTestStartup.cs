using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.WebApp.Application
{
    public class WebAppTestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplication<WebAppTestAElfModule>();
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