using System;
using System.Collections.Generic;
using AElf.Modularity;
using AElf.Providers;
using Volo.Abp.Modularity;

namespace AElf
{
    [DependsOn(
        typeof(CoreAElfModule))]
    public class CoreAElfTestModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
    
    [DependsOn(
        typeof(CoreAElfModule))]
    public class CoreWithServiceContainerFactoryOptionsAElfTestModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ServiceContainerFactoryOptions<ITestProvider>>(options =>
                {
                    options.Types = new List<Type>
                        {typeof(ATestProvider), typeof(CTestProvider), typeof(BTestProvider)};
                });
        }
    }
}