using System.Collections.Generic;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Contracts.MultiToken
{
    public class BasicFunctionContractInitializationProvider : IContractInitializationProvider, ITransientDependency
    {
        public Hash SystemSmartContractName { get; } = HashHelper.ComputeFrom("AElf.TestContractNames.BasicFunction");
        public string ContractCodeName { get; } = "AElf.Contracts.TestContract.BasicFunction";

        public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
        {
            return new List<ContractInitializationMethodCall>();
        }
    }
}