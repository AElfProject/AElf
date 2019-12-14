using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public class SmartContractService : ISmartContractService, ITransientDependency
    {
        private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;

        private readonly ISmartContractAddressService _smartContractAddressService;

        public SmartContractService(
            ISmartContractRunnerContainer smartContractRunnerContainer,
            ISmartContractAddressService smartContractAddressService)
        {
            _smartContractRunnerContainer = smartContractRunnerContainer;
            _smartContractAddressService = smartContractAddressService;
        }

        /// <inheritdoc/>
        public Task DeployContractAsync(ContractDto contractDto)
        {
            // get runner
            var runner = _smartContractRunnerContainer.GetRunner(contractDto.SmartContractRegistration.Category);

            if (contractDto.ContractName != null)
                _smartContractAddressService.SetAddress(contractDto.ContractName, contractDto.ContractAddress);
            return Task.CompletedTask;
        }

        public Task UpdateContractAsync(ContractDto contractDto)
        {
            // get runner
            var runner = _smartContractRunnerContainer.GetRunner(contractDto.SmartContractRegistration.Category);
            return Task.CompletedTask;
        }

    }
}