using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Database;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.SmartContractExecution.Scheduling;
using AElf.Modularity;
using AElf.TestBase;
using Castle.DynamicProxy.Generators;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContractExecution
{
    [DependsOn(
        typeof(SmartContractExecutionAElfModule),
        typeof(KernelCoreTestAElfModule)
    )]
    public class SmartContractExecutionTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {

        }
    }
}