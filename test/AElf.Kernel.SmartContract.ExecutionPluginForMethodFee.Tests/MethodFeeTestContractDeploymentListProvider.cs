using System.Collections.Generic;
using AElf.Kernel.SmartContractInitialization;
using AElf.Kernel.Token;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests
{
    public class MethodFeeTestContractDeploymentListProvider : IContractDeploymentListProvider, ITransientDependency
    {
        public List<Hash> GetDeployContractNameList()
        {
            return new List<Hash>
            {
                TokenSmartContractAddressNameProvider.Name,
            };
        }
    }
}