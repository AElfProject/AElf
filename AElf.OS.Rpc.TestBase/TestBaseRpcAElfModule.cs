using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Genesis;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Modularity;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Node.Application;
using AElf.OS.Rpc.ChainController;
using AElf.OS.Rpc.Net;
using AElf.OS.Rpc.Wallet;
using AElf.OS.Tests;
using AElf.Runtime.CSharp;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp;
using Volo.Abp.AspNetCore.TestBase;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.OS.Rpc
{
    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(AbpAspNetCoreTestBaseModule),
        typeof(ChainControllerRpcModule),
        typeof(WalletRpcModule),
        typeof(NetRpcAElfModule),
        typeof(TestsOSAElfModule)
    )]
    public class TestBaseRpcAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            
        }
    }
}