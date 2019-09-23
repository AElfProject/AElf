using AElf.CSharp.Core;
using AElf.Kernel.Blockchain.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.Application
{
    public abstract class ContractEventDiscoveryServiceBase<T> where T : IEvent<T>, new()
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionResultQueryService _transactionResultQueryService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        public ILogger<ContractEventDiscoveryServiceBase<T>> Logger { get; set; }
        public ILocalEventBus LocalEventBus { get; set; }
        private Address _contractAddress;
        
        private LogEvent _logEvent;
        private Bloom _bloom;

        public ContractEventDiscoveryServiceBase(IBlockchainService blockchainService,
            ITransactionResultQueryService transactionResultQueryService,
            ISmartContractAddressService smartContractAddressService)
        {
            _blockchainService = blockchainService;
            _transactionResultQueryService = transactionResultQueryService;
            _smartContractAddressService = smartContractAddressService;
            LocalEventBus = NullLocalEventBus.Instance;
            Logger = NullLogger<ContractEventDiscoveryServiceBase<T>>.Instance;
        }

        private void PrepareBloom()
        {
            if (_bloom != null)
            {
                // already prepared
                return;
            }

            _logEvent = new T().ToLogEvent(_contractAddress);
            _bloom = _logEvent.GetBloom();
        }

        public abstract void SetContractAddress();
    }
}