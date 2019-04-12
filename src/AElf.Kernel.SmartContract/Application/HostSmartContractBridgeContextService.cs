using System;
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


        public IHostSmartContractBridgeContext Create()
        {
            //Create a new context
            var context = _serviceProvider.GetService<IHostSmartContractBridgeContext>();
            return context;
        }
    }
}