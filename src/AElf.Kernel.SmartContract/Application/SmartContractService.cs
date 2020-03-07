using System.Threading.Tasks;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public class SmartContractService : ISmartContractService, ITransientDependency
    {
        private readonly ISmartContractChangeHeightProvider _smartContractChangeHeightProvider;
        private readonly ISmartContractAddressService _smartContractAddressService;

        public SmartContractService(ISmartContractAddressService smartContractAddressService, 
            ISmartContractChangeHeightProvider smartContractChangeHeightProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _smartContractChangeHeightProvider = smartContractChangeHeightProvider;
        }

        /// <inheritdoc/>
        public Task DeployContractAsync(ContractDto contractDto)
        {
            _smartContractChangeHeightProvider.AddSmartContractChangeHeight(contractDto.ContractAddress,
                contractDto.BlockHeight);
            if (contractDto.ContractName != null)
                _smartContractAddressService.SetAddress(contractDto.ContractName, contractDto.ContractAddress);
            return Task.CompletedTask;
        }

        public Task UpdateContractAsync(ContractDto contractDto)
        {
            _smartContractChangeHeightProvider.AddSmartContractChangeHeight(contractDto.ContractAddress,
                contractDto.BlockHeight);
            return Task.CompletedTask;
        }

    }
}