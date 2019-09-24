using System.Threading.Tasks;
using Acs0;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.TransactionPool.Application
{
    internal class BestChainFoundEventHandler : ILocalEventHandler<BestChainFoundEventData>, ITransientDependency
    {
        private readonly IDeployedContractAddressProvider _deployedContractAddressProvider;
        private readonly ContractEventDiscoveryService<ContractDeployed> _contractDeployDiscoveryService;
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ILogger<BestChainFoundEventHandler> Logger { get; set; }

        public BestChainFoundEventHandler(IDeployedContractAddressProvider deployedContractAddressProvider,
            ContractEventDiscoveryService<ContractDeployed> contractDeployDiscoveryService,
            ISmartContractAddressService smartContractAddressService)
        {
            _deployedContractAddressProvider = deployedContractAddressProvider;
            _contractDeployDiscoveryService = contractDeployDiscoveryService;
            _smartContractAddressService = smartContractAddressService;

            Logger = NullLogger<BestChainFoundEventHandler>.Instance;
        }

        public async Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            var zeroContractAddress = _smartContractAddressService.GetZeroSmartContractAddress();
            foreach (var executedBlockHash in eventData.ExecutedBlocks)
            {
                var deployedContracts =
                    await _contractDeployDiscoveryService.GetEventMessagesAsync(executedBlockHash, zeroContractAddress);
                foreach (var deployedContract in deployedContracts)
                {
                    if (deployedContract.Address == null) continue;
                    _deployedContractAddressProvider.AddDeployedContractAddress(deployedContract.Address);
                    Logger.LogTrace($"Added deployed contract address of {deployedContract}");
                }
            }
        }
    }
}