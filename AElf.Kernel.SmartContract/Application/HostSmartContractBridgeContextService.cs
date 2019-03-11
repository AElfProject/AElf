using System;
using AElf.Kernel.SmartContract.Contexts;
using AElf.Kernel.SmartContract.Sdk;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public class HostSmartContractBridgeContextService : IHostSmartContractBridgeContextService
    {
        private readonly IServiceProvider _serviceProvider;

        public HostSmartContractBridgeContextService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }


        public IHostSmartContractBridgeContext Create(ISmartContractContext smartContractContext)
        {
            var smartContractBridgeService = _serviceProvider.GetService<ISmartContractBridgeService>();
            var smartContractExecutiveService = _serviceProvider.GetService<ISmartContractExecutiveService>();
            var context =
                new HostSmartContractBridgeContext(smartContractBridgeService, smartContractExecutiveService);

            context.SmartContractContext = smartContractContext;
            return context;
        }
    }
}