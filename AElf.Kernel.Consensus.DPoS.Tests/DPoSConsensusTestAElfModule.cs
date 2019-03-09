using System.Collections.Generic;
using System.IO;
using AElf.Common;
using AElf.Contracts.Genesis;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Database;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Modularity;
using AElf.OS;
using AElf.OS.Network.Infrastructure;
using AElf.Runtime.CSharp;
using AElf.TestBase;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Consensus.DPoS.Tests
{
    [DependsOn(
        typeof(KernelCoreTestAElfModule),
        typeof(DPoSConsensusAElfModule),
        typeof(CSharpRuntimeAElfModule),
        typeof(CoreOSAElfModule)
    )]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once ClassNeverInstantiated.Global
    public class DPoSConsensusTestAElfModule : TestBaseAElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton(o => Mock.Of<IAElfNetworkServer>());
        }
    }
}