﻿using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
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

            _smartContractExecutiveService.ClearExecutivePool(contractAddress);

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