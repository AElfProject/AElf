using System;
using AElf.Configuration.Config.Network;
using AElf.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

namespace AElf.RPC.Tests
{
    public class RpcTestStartup
    {
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddApplication<TestsRpcAElfModule>(options =>
            {
                options.UseAutofac();
            });

            return services.BuildAutofacServiceProvider();
        }

        public void Configure(IApplicationBuilder app)
        {
            NetworkConfig.Instance.EcKeyPair = CryptoHelpers.GenerateKeyPair();
            
            app.InitializeApplication();
            
            app.UseCors(builder =>
                builder.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());
        }
    }
}