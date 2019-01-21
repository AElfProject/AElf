using AElf.Common;
using AElf.Kernel;

// ReSharper disable once CheckNamespace
namespace AElf.SmartContract
{
    public class SmartContractContext : ISmartContractContext
    {
        public int ChainId { get; set; }
        public Address ContractAddress { get; set; }
        public IDataProvider DataProvider { get; set; }
        public ISmartContractService SmartContractService { get; set; }
        public IChainService ChainService { get; set; }
    }
}
