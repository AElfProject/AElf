using AElf.Management.Interfaces;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Modularity;

namespace AElf.Management.Website
{
    [DependsOn(typeof(AbpAspNetCoreMvcModule),
        typeof(ManagementAElfModule))]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ManagementWebsiteAElfModule : AElfModule
    {
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var recordService = context.ServiceProvider.GetRequiredService<IRecordService>();
            recordService.Start();
        }
    }
}