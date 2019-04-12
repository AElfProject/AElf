using System.Collections.Concurrent;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.Types;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.Application
{
    //TODO: remove _executivePools, _contractHashs, change ISingletonDependency to ITransientDependency
    public class SmartContractService : ISmartContractService, ISingletonDependency
    {
        private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;

        private readonly IFunctionMetadataService _functionMetadataService;
        private readonly IBlockchainService _chainService;
        private readonly ISmartContractAddressService _smartContractAddressService;

        public SmartContractService(
            ISmartContractRunnerContainer smartContractRunnerContainer,
            IFunctionMetadataService functionMetadataService, IBlockchainService chainService,
            ISmartContractAddressService smartContractAddressService)
        {
            _smartContractRunnerContainer = smartContractRunnerContainer;
            _functionMetadataService = functionMetadataService;
            _chainService = chainService;
            _smartContractAddressService = smartContractAddressService;
        }

        /// <inheritdoc/>
        public async Task DeployContractAsync(Address contractAddress,
            SmartContractRegistration registration, bool isPrivileged, Hash name)
        {
            // get runner
            var runner = _smartContractRunnerContainer.GetRunner(registration.Category);
            await Task.Run(() => runner.CodeCheck(registration.Code.ToByteArray(), isPrivileged));

            if (name != null)
                _smartContractAddressService.SetAddress(name, contractAddress);

            //Todo New version metadata handle it
//            var contractType = runner.GetContractType(registration);
//            var contractTemplate = runner.ExtractMetadata(contractType);
//            await _functionMetadataService.DeployContract(contractAddress, contractTemplate);
        }

        public async Task UpdateContractAsync(Address contractAddress,
            SmartContractRegistration newRegistration, bool isPrivileged, Hash name)
        {
            // get runner
            var runner = _smartContractRunnerContainer.GetRunner(newRegistration.Category);
            await Task.Run(() => runner.CodeCheck(newRegistration.Code.ToByteArray(), isPrivileged));

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