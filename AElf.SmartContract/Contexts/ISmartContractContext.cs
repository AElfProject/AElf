using System;
using AElf.Kernel.Types;
using AElf.Kernel;

namespace AElf.SmartContract
{
    public interface ISmartContractContext
    {
        Hash ChainId { get; set; }
        Hash ContractAddress { get; set; }
        ICachedDataProvider DataProvider { get; set; }
        ISmartContractService SmartContractService { get; set; }
    }
}
