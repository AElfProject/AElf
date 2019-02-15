using AElf.Common;
using AElf.Common.Enums;
using AElf.Common.MultiIndexDictionary;
using AElf.Common.Serializers;
using AElf.Database;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.ChainController;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContractExecution.Infrastructure;
using AElf.Kernel.TransactionPool;
using AElf.Kernel.Types;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Kernel
{
    [DependsOn(typeof(CoreKernelAElfModule), typeof(ChainControllerAElfModule), typeof(SmartContractAElfModule),
        typeof(TransactionPoolAElfModule))]
    public class KernelAElfModule : AElfModule<KernelAElfModule>
    {
    }
}