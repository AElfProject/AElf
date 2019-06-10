using System;
using System.Threading.Tasks;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    [DependsOn(typeof(OSTestAElfModule))]
    public class BlockSyncTestBaseAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<ITaskQueue>(o =>
            {
                var taskQueue = new Mock<ITaskQueue>();
                taskQueue.Setup(t => t.Enqueue(It.IsAny<Func<Task>>())).Callback<Func<Task>>(async task =>
                {
                    await task();
                });

                return taskQueue.Object;
            });
        }
    }
}