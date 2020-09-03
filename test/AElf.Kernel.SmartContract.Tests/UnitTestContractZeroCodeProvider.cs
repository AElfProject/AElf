using System;
using AElf.ContractDeployer;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;

namespace AElf.Kernel.SmartContract
{
    public class UnitTestContractZeroCodeProvider : DefaultContractZeroCodeProvider
    {
        public UnitTestContractZeroCodeProvider(IStaticChainInformationProvider staticChainInformationProvider,
            IOptionsSnapshot<ContractOptions> contractOptions) : base(staticChainInformationProvider, contractOptions)
        {
        }

        public override void SetDefaultContractZeroRegistrationByType(Type defaultZero)
        {
            var codes = ContractsDeployer.GetContractCodes<SmartContractTestAElfModule>();
            DefaultContractZeroRegistration=  new SmartContractRegistration
            {
                Category = GetCategory(),
                Code = ByteString.CopyFrom(codes["AElf.Contracts.Genesis"]),
                CodeHash = HashHelper.ComputeFrom(codes["AElf.Contracts.Genesis"])
            };
        }
    }
}