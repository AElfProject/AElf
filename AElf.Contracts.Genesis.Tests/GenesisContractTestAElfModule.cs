using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Database;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Genesis
{
    public class BasicContractZeroTestAElfModule : ContractTestAElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {

        }
    }
}