using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public class SmartContractService : ISmartContractService, ITransientDependency
    {
        private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;
        private readonly ISmartContractAddressService _smartContractAddressService;

        public SmartContractService(
            ISmartContractRunnerContainer smartContractRunnerContainer,
            ISmartContractAddressService smartContractAddressService, 
            ISmartContractExecutiveService smartContractExecutiveService)
        {
            _smartContractRunnerContainer = smartContractRunnerContainer;
            _smartContractAddressService = smartContractAddressService;
            _smartContractExecutiveService = smartContractExecutiveService;
        }

        /// <inheritdoc/>
        public Task DeployContractAsync(ContractDto contractDto)
        {
            _smartContractExecutiveService.AddContractInfo(contractDto.ContractAddress, contractDto.BlockHeight);
            if (contractDto.ContractName != null)
                _smartContractAddressService.SetAddress(contractDto.ContractName, contractDto.ContractAddress);
            return Task.CompletedTask;
        }

        public Task UpdateContractAsync(ContractDto contractDto)
        {
            _smartContractExecutiveService.AddContractInfo(contractDto.ContractAddress, contractDto.BlockHeight);
            return Task.CompletedTask;
        }

    }
}