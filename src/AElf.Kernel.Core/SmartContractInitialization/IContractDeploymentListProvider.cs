using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.SmartContractInitialization
{
    public interface IContractDeploymentListProvider
    {
        List<Hash> GetDeployContractNameList();
    }
}