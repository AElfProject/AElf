using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.TransactionPool.Application
{
    internal class BestChainFoundEventHandler : ILocalEventHandler<BestChainFoundEventData>, ITransientDependency
    {
        private readonly IDeployedContractAddressProvider _deployedContractAddressProvider;
        private readonly IContractDeployDiscoveryService _contractDeployDiscoveryService;

        private readonly IBlockchainService _blockchainService;

        public ILogger<BestChainFoundEventHandler> Logger { get; set; }

        public BestChainFoundEventHandler(IDeployedContractAddressProvider deployedContractAddressProvider,
            IBlockchainService blockchainService, IContractDeployDiscoveryService contractDeployDiscoveryService)
        {
            _deployedContractAddressProvider = deployedContractAddressProvider;
            _blockchainService = blockchainService;
            _contractDeployDiscoveryService = contractDeployDiscoveryService;

            Logger = NullLogger<BestChainFoundEventHandler>.Instance;
        }

        public async Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            var chain = await _blockchainService.GetChainAsync();
            var address = await _contractDeployDiscoveryService.GetDeployedContractAddress(chain,
                eventData.ExecutedBlocks);
            _deployedContractAddressProvider.AddDeployedContractAddress(address);
        }
    }
}