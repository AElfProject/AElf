using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.ContractsInitialization
{
    public interface IContractDeploymentListProvider
    {
        List<Hash> GetDeployContractNameList();
    }
}