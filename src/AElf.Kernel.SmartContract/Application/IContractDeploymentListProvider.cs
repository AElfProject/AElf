using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IContractDeploymentListProvider
    {
        List<Hash> GetDeployContractNameList();
    }
}