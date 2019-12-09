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
            //Todo New version metadata handle it
//            var contractType = runner.GetContractType(registration);
//            var contractTemplate = runner.ExtractMetadata(contractType);
//            await _functionMetadataService.DeployContract(contractAddress, contractTemplate);
            return Task.CompletedTask;
        }

        public Task UpdateContractAsync(ContractDto contractDto)
        {
            // get runner
            var runner = _smartContractRunnerContainer.GetRunner(contractDto.SmartContractRegistration.Category);

            //Todo New version metadata handle it
//            var oldRegistration = await GetContractByAddressAsync(contractAddress);
//            var oldContractType = runner.GetContractType(oldRegistration);
//            var oldContractTemplate = runner.ExtractMetadata(oldContractType);
//
//            var newContractType = runner.GetContractType(newRegistration);
//            var newContractTemplate = runner.ExtractMetadata(newContractType);
//            await _functionMetadataService.UpdateContract(contractAddress, newContractTemplate,
//                oldContractTemplate);
            return Task.CompletedTask;
        }

    }
}