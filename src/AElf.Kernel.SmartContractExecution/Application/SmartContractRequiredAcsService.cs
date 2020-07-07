using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Configuration;
using AElf.Kernel.Configuration;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class SmartContractRequiredAcsService : ISmartContractRequiredAcsService
    {
        private readonly IConfigurationDataService _configurationDataService;

        public SmartContractRequiredAcsService(IConfigurationDataService configurationDataService)
        {
            _configurationDataService = configurationDataService;
        }

        public async Task<RequiredAcs> GetRequiredAcsInContractsAsync(Hash blockHash, long blockHeight)
        {
            var chainContext = new ChainContext
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            };

            var returned =
                await _configurationDataService.GetConfigurationDataAsync(
                    RequiredAcsInContractsConfigurationNameProvider.Name, chainContext);

            var requiredAcsInContracts = new RequiredAcsInContracts();
            requiredAcsInContracts.MergeFrom(returned);
            return new RequiredAcs
            {
                AcsList = requiredAcsInContracts.AcsList.ToList(),
                RequireAll = requiredAcsInContracts.RequireAll
            };
        }
    }
}