using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.TransactionPool.Application
{
    internal class NewIrreversibleBlockFoundEventHandler : ILocalEventHandler<NewIrreversibleBlockFoundEvent>,
        ITransientDependency
    {
        private readonly ContractEventDiscoveryService<TransactionSizeFeeUnitPriceUpdated>
            _transactionSizeFeeUnitPriceUpdatedDiscoveryService;

        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITransactionSizeFeeUnitPriceProvider _transactionSizeFeeUnitPriceProvider;

        public NewIrreversibleBlockFoundEventHandler(
            ContractEventDiscoveryService<TransactionSizeFeeUnitPriceUpdated>
                transactionSizeFeeUnitPriceUpdatedDiscoveryService,
            ISmartContractAddressService smartContractAddressService,
            ITransactionSizeFeeUnitPriceProvider transactionSizeFeeUnitPriceProvider)
        {
            _transactionSizeFeeUnitPriceUpdatedDiscoveryService = transactionSizeFeeUnitPriceUpdatedDiscoveryService;
            _smartContractAddressService = smartContractAddressService;
            _transactionSizeFeeUnitPriceProvider = transactionSizeFeeUnitPriceProvider;
        }

        public async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            var tokenContractAddress =
                _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);
            var txFeeUnitPrice =
                (await _transactionSizeFeeUnitPriceUpdatedDiscoveryService.GetEventMessagesAsync(eventData.BlockHash,
                    tokenContractAddress)).FirstOrDefault()?.UnitPrice;
            _transactionSizeFeeUnitPriceProvider.SetUnitPrice(txFeeUnitPrice ?? 0);
        }
    }
}