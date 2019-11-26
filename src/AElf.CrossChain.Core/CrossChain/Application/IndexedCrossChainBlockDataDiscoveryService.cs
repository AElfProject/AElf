using AElf.Contracts.CrossChain;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    public class IndexedCrossChainBlockDataDiscoveryService : IIndexedCrossChainBlockDataDiscoveryService, ITransientDependency
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        
        public IndexedCrossChainBlockDataDiscoveryService(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }
        
        public bool TryDiscoverIndexedParentChainBlockDataAsync(IBlock block)
        {
            var crossChainContractAddress = GetCrossChainContractAddress();
            return new ParentChainBlockDataIndexed().ToLogEvent(crossChainContractAddress).GetBloom()
                .IsIn(new Bloom(block.Header.Bloom.ToByteArray()));
        }

        public bool TryDiscoverIndexedSideChainBlockDataAsync(IBlock block)
        {
            var crossChainContractAddress = GetCrossChainContractAddress();
            return new SideChainBlockDataIndexed().ToLogEvent(crossChainContractAddress).GetBloom()
                .IsIn(new Bloom(block.Header.Bloom.ToByteArray()));
        }

        private Address GetCrossChainContractAddress()
        {
            return _smartContractAddressService.GetAddressByContractName(
                CrossChainSmartContractAddressNameProvider.Name);
        }
    }
}