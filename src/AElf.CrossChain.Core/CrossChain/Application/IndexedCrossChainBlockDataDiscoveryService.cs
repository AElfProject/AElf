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
        private Bloom _parentChainBlockIndexedEventBloom;
        private Bloom _sideChainBlockDataIndexedEventBloom;
        private Address _crossChainContractAddress;

        private readonly ISmartContractAddressService _smartContractAddressService;
        
        public IndexedCrossChainBlockDataDiscoveryService(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }
        
        public bool TryDiscoverIndexedParentChainBlockDataAsync(IBlock block)
        {
            SetEventBloom();
            return _parentChainBlockIndexedEventBloom.IsIn(new Bloom(block.Header.Bloom.ToByteArray()));
        }

        public bool TryDiscoverIndexedSideChainBlockDataAsync(IBlock block)
        {
            SetEventBloom();
            return _sideChainBlockDataIndexedEventBloom.IsIn(new Bloom(block.Header.Bloom.ToByteArray()));
        }

        private void SetEventBloom()
        {
            if (_crossChainContractAddress == null)
                _crossChainContractAddress =
                    _smartContractAddressService.GetAddressByContractName(CrossChainSmartContractAddressNameProvider
                        .Name);
            if (_parentChainBlockIndexedEventBloom == null)
                _parentChainBlockIndexedEventBloom = new ParentChainBlockDataIndexed()
                    .ToLogEvent(_crossChainContractAddress).GetBloom();
            if (_sideChainBlockDataIndexedEventBloom == null)
                _sideChainBlockDataIndexedEventBloom =
                    new SideChainBlockDataIndexed().ToLogEvent(_crossChainContractAddress).GetBloom();
        }
    }
}