using AElf.Types;

namespace AElf.ContractTestBase.ContractTestKit
{
    public class ChainInitializationOptions
    {
        public int ChainId { get; set; }
        public string Symbol { get; set; }
        public Address ParentChainTokenContractAddress { get; set; }
        public int ParentChainId { get; set; }
        public long CreationHeightOnParentChain { get; set; }
        public bool RegisterParentChainTokenContractAddress { get; set; } = true;
    }
}