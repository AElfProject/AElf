using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    //TODO: remove _executivePools, _contractHashs, change ISingletonDependency to ITransientDependency
    public class SmartContractService : ISmartContractService, ISingletonDependency
    {
        private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;

        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;

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
        public async Task DeployContractAsync(ContractDto contractDto)
        {
            // get runner
            var runner = _smartContractRunnerContainer.GetRunner(contractDto.SmartContractRegistration.Category);
            await Task.Run(() => runner.CodeCheck(contractDto.SmartContractRegistration.Code.ToByteArray(), contractDto.IsPrivileged));

            if (contractDto.ContractName != null)
                _smartContractAddressService.SetAddress(contractDto.ContractName, contractDto.ContractAddress);
            await _smartContractExecutiveService.SetContractInfoAsync(contractDto.ContractAddress,
                contractDto.BlockHeight);

            //Todo New version metadata handle it
//            var contractType = runner.GetContractType(registration);
//            var contractTemplate = runner.ExtractMetadata(contractType);
//            await _functionMetadataService.DeployContract(contractAddress, contractTemplate);
        }

        public async Task UpdateContractAsync(ContractDto contractDto)
        {
            // get runner
            var runner = _smartContractRunnerContainer.GetRunner(contractDto.SmartContractRegistration.Category);
            await Task.Run(() => runner.CodeCheck(contractDto.SmartContractRegistration.Code.ToByteArray(),
                contractDto.IsPrivileged));

            await _smartContractExecutiveService.SetContractInfoAsync(contractDto.ContractAddress,
                contractDto.BlockHeight);

            //Todo New version metadata handle it
//            var oldRegistration = await GetContractByAddressAsync(contractAddress);
//            var oldContractType = runner.GetContractType(oldRegistration);
//            var oldContractTemplate = runner.ExtractMetadata(oldContractType);
//
//            var newContractType = runner.GetContractType(newRegistration);
//            var newContractTemplate = runner.ExtractMetadata(newContractType);
//            await _functionMetadataService.UpdateContract(contractAddress, newContractTemplate,
//                oldContractTemplate);
        }

    }
}