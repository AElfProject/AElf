using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Blockchains.SideChain
{
//    internal class BestChainFoundEventHandler : ILocalEventHandler<BestChainFoundEventData>, ITransientDependency
//    {
//        private readonly ContractEventDiscoveryService<ChainPrimaryTokenSymbolSet> _primaryTokenSymbolDiscoveryService;
//        private readonly IPrimaryTokenSymbolProvider _primaryTokenSymbolProvider;
//        private readonly ISmartContractAddressService _smartContractAddressService;
//
//        public ILogger<BestChainFoundEventHandler> Logger { get; set; }
//
//        public BestChainFoundEventHandler(
//            ContractEventDiscoveryService<ChainPrimaryTokenSymbolSet> primaryTokenSymbolDiscoveryService,
//            IPrimaryTokenSymbolProvider primaryTokenSymbolProvider,
//            ISmartContractAddressService smartContractAddressService)
//        {
//            _primaryTokenSymbolDiscoveryService = primaryTokenSymbolDiscoveryService;
//            _primaryTokenSymbolProvider = primaryTokenSymbolProvider;
//            _smartContractAddressService = smartContractAddressService;
//
//            Logger = NullLogger<BestChainFoundEventHandler>.Instance;
//        }
//
//        public async Task HandleEventAsync(BestChainFoundEventData eventData)
//        {
//            var tokenContractAddress =
//                _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);
//            if (eventData.BlockHeight == Constants.GenesisBlockHeight)
//            {
//                var symbol =
//                    (await _primaryTokenSymbolDiscoveryService.GetEventMessagesAsync(eventData.BlockHash,
//                        tokenContractAddress))
//                    .FirstOrDefault();
//                _primaryTokenSymbolProvider.SetPrimaryTokenSymbol(symbol?.TokenSymbol);
//
//                Logger.LogInformation($"Primary token symbol for current chain: {symbol}");
//            }
//        }
//    }
}