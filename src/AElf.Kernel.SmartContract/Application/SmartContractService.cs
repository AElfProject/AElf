using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public class SmartContractService : ISmartContractService, ITransientDependency
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;

        public SmartContractService(ISmartContractAddressService smartContractAddressService,
            ISmartContractRunnerContainer smartContractRunnerContainer)
        {
            _smartContractAddressService = smartContractAddressService;
            _smartContractRunnerContainer = smartContractRunnerContainer;
        }

        /// <inheritdoc/>
        public Task DeployContractAsync(ContractDto contractDto)
        {
            CheckRunner(contractDto.SmartContractRegistration.Category);
            return Task.CompletedTask;
        }

        public Task UpdateContractAsync(ContractDto contractDto)
        {
            return Task.CompletedTask;
        }

        private void CheckRunner(int category)
        {
            _smartContractRunnerContainer.GetRunner(category);
        }
    }
}