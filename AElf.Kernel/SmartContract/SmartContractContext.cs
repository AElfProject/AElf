using System;
using AElf.Kernel.Services;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public class SmartContractContext : ISmartContractContext
    {
        public Hash ChainId { get; set; }
        public Hash ContractAddress { get; set; }
        public IDataProvider DataProvider { get; set; }
        public ISmartContractService SmartContractService { get; set; }
    }
}
