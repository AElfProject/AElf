using AElf.Contracts.CrossChain;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;

namespace AElf.CrossChain
{
    public class IndexedCrossChainBlockDataDiscoveryService : IIndexedCrossChainBlockDataDiscoveryService
    {
        private readonly Bloom _bloom;
        
        public IndexedCrossChainBlockDataDiscoveryService(ISmartContractAddressService smartContractAddressService)
        {
            var contractAddress = smartContractAddressService.GetAddressByContractName(CrossChainSmartContractAddressNameProvider.Name);
            var logEvent = new CrossChainBlockDataIndexed().ToLogEvent(contractAddress);
            _bloom = logEvent.GetBloom();
        }

        public bool TryDiscoverCrossChainBlockDataAsync(IBlock block)
        {
            return _bloom.IsIn(new Bloom(block.Header.Bloom.ToByteArray()));
        }
    }
}