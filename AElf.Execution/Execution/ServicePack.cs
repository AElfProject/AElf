using AElf.ChainController;
using AElf.SmartContract;

namespace AElf.Execution
{
    public class ServicePack
    {
        public IResourceUsageDetectionService ResourceDetectionService { get; set; }
        public ISmartContractService SmartContractService { get; set; }
        public IChainContextService ChainContextService { get; set; }
        public IAccountContextService AccountContextService { get; set; }
        public IWorldStateDictator WorldStateDictator { get; set; }
    }
}
