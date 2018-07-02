using System;
using AElf.Kernel.Services;
using AElf.Kernel.Types;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public class SmartContractContext : ISmartContractContext
    {
        public Hash ChainId { get; set; }
        public Hash ContractAddress { get; set; }
        public ICachedDataProvider DataProvider { get; set; }
        public ISmartContractService SmartContractService { get; set; }
    }
}
