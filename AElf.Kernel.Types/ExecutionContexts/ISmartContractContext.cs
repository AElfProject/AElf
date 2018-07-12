using System;
using AElf.Kernel.Services;
using AElf.Kernel.Types;

namespace AElf.Kernel
{
    public interface ISmartContractContext
    {
        Hash ChainId { get; set; }
        Hash ContractAddress { get; set; }
        ITentativeDataProvider DataProvider { get; set; }
        ISmartContractService SmartContractService { get; set; }
    }
}
