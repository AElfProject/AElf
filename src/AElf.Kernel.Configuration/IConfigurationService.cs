using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Configuration;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Configuration
{
    public interface IConfigurationService
    {
        Task ProcessConfigurationAsync(string configurationName, ByteString byteString, BlockIndex blockIndex);
        Task<ByteString> GetConfigurationDataAsync(string configurationName, ChainContext chainContext);
    }

    internal class ConfigurationService : IConfigurationService, ITransientDependency
    {
        private readonly List<IConfigurationProcessor> _configurationProcessors;
        private readonly IContractReaderFactory<ConfigurationContainer.ConfigurationStub> _contractReaderFactory;
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ConfigurationService(IEnumerable<IConfigurationProcessor> configurationProcessors, 
            IContractReaderFactory<ConfigurationContainer.ConfigurationStub> contractReaderFactory, 
            ISmartContractAddressService smartContractAddressService)
        {
            _contractReaderFactory = contractReaderFactory;
            _smartContractAddressService = smartContractAddressService;
            _configurationProcessors = configurationProcessors.ToList();
        }

        public async Task ProcessConfigurationAsync(string configurationName, ByteString byteString, BlockIndex blockIndex)
        {
            var configurationProcessor =
                _configurationProcessors.FirstOrDefault(c => c.ConfigurationName == configurationName);
            if (configurationProcessor == null)
                return;
            await configurationProcessor.ProcessConfigurationAsync(byteString, blockIndex);
        }
        
        public async Task<ByteString> GetConfigurationDataAsync(string configurationName, ChainContext chainContext)
        {
            var indexedSideChainBlockData = await _contractReaderFactory
                .Create(new ContractReaderContext
                {
                    BlockHash = chainContext.BlockHash,
                    BlockHeight = chainContext.BlockHeight,
                    ContractAddress = await _smartContractAddressService.GetAddressByContractNameAsync(chainContext,
                        ConfigurationSmartContractAddressNameProvider.StringName)
                })
                .GetConfiguration.CallAsync(new StringValue() {Value = configurationName});

            return indexedSideChainBlockData.Value;
        }
    }
}