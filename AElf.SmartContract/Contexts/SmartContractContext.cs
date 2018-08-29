using System;
using AElf.Kernel.Types;
using AElf.Kernel;
// ReSharper disable once CheckNamespace
namespace AElf.SmartContract
{
    public class SmartContractContext : ISmartContractContext
    {
        public Hash ChainId { get; set; }
        public Hash ContractAddress { get; set; }
        public IDataProvider DataProvider { get; set; }
        public ISmartContractService SmartContractService { get; set; }
    }
}
