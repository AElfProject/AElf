using System.Collections.Generic;
using AElf.Contracts.TestBase;
using AElf.Kernel.Blockchain.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contract.CrossChain.Tests
{
    [DependsOn(
        typeof(ContractTestAElfModule)
    )]
    public class CrossChainContractTestAElfModule : AElfModule
    {

    }
}