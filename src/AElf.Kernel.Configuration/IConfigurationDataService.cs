using System.Threading.Tasks;
using AElf.Contracts.Configuration;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Configuration
{
    public interface IConfigurationDataService
    {
        Task<ByteString> GetConfigurationDataAsync(string configurationName, ChainContext chainContext);
    }

    public class ConfigurationDataService : IConfigurationDataService
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        private readonly IContractReaderFactory<ConfigurationContainer.ConfigurationStub> _contractReaderFactory;

        internal ConfigurationDataService(ISmartContractAddressService smartContractAddressService,
            IContractReaderFactory<ConfigurationContainer.ConfigurationStub> contractReaderFactory)
        {
            _smartContractAddressService = smartContractAddressService;
            _contractReaderFactory = contractReaderFactory;
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