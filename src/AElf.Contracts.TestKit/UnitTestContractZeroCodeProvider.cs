using AElf.Kernel;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using Microsoft.Extensions.Options;

namespace AElf.Contracts.TestKit
{
    public class UnitTestContractZeroCodeProvider : DefaultContractZeroCodeProvider
    {
        public UnitTestContractZeroCodeProvider(IStaticChainInformationProvider staticChainInformationProvider,
            IOptionsSnapshot<ContractOptions> contractOptions) : base(staticChainInformationProvider, contractOptions)
        {
        }

        protected override int GetCategory()
        {
            return KernelConstants.CodeCoverageRunnerCategory;
        }
    }
}