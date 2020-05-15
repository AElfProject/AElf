using System;
using System.Collections.Generic;
using AElf.Modularity;
using AElf.Providers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf
{
    [DependsOn(
        typeof(CoreAElfModule))]
    public class CoreAElfTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.Configure<ServiceContainerFactoryOptions<ITestProvider>>(o =>
            {
                o.Types = new List<Type>
                {
                    typeof(ATestProvider), 
                    typeof(BTestProvider),
                    typeof(CTestProvider)
                };
            });

            services.AddSingleton(typeof(ITestProvider),
                typeof(CTestProvider));
            services.AddSingleton(typeof(ITestProvider),
                typeof(CTestProvider));
            services.AddSingleton(typeof(ITestProvider),
                typeof(ATestProvider));
        }
    }
}