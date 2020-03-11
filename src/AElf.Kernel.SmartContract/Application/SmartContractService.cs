using System.Threading.Tasks;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public class SmartContractService : ISmartContractService, ITransientDependency
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public SmartContractService(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        /// <inheritdoc/>
        public Task DeployContractAsync(ContractDto contractDto)
        {
            if (contractDto.ContractName != null)
                _smartContractAddressService.SetAddress(contractDto.ContractName, contractDto.ContractAddress);
            return Task.CompletedTask;
        }

        public Task UpdateContractAsync(ContractDto contractDto)
        {
            return Task.CompletedTask;
        }

    }
}