using System;
using AElf.Kernel.Services;

namespace AElf.Kernel
{
    public interface ISmartContractContext
    {
        Hash ChainId { get; set; }
        Hash ContractAddress { get; set; }
        ICachedDataProvider DataProvider { get; set; }
        ISmartContractService SmartContractService { get; set; }
    }
}
