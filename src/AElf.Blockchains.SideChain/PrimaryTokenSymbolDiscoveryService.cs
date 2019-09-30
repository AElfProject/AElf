using System.Threading.Tasks;
using Acs0;
using AElf.Contracts.MultiToken;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Blockchains.SideChain
{
    public class PrimaryTokenSymbolDiscoveryService : IPrimaryTokenSymbolDiscoveryService, ITransientDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionResultQueryService _transactionResultQueryService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        public ILogger<PrimaryTokenSymbolDiscoveryService> Logger { get; set; }
        public ILocalEventBus LocalEventBus { get; set; }
        private Address _contractAddress;
        private ChainPrimaryTokenSymbolSet _interestedEvent;
        private LogEvent _logEvent;
        private Bloom _bloom;

        public PrimaryTokenSymbolDiscoveryService(IBlockchainService blockchainService,
            ITransactionResultQueryService transactionResultQueryService,
            ISmartContractAddressService smartContractAddressService)
        {
            _blockchainService = blockchainService;
            _transactionResultQueryService = transactionResultQueryService;
            _smartContractAddressService = smartContractAddressService;
            LocalEventBus = NullLocalEventBus.Instance;
            Logger = NullLogger<PrimaryTokenSymbolDiscoveryService>.Instance;
        }

        private void PrepareBloom()
        {
            if (_bloom != null)
            {
                // already prepared
                return;
            }

            _contractAddress =
                _smartContractAddressService.GetZeroSmartContractAddress();
            _interestedEvent = new ChainPrimaryTokenSymbolSet();
            _logEvent = _interestedEvent.ToLogEvent(_contractAddress);
            _bloom = _logEvent.GetBloom();
        }

        public async Task<string> GetPrimaryTokenSymbol()
        {
            PrepareBloom();

            var blockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync();
            if (blockHeader.Height != Constants.GenesisBlockHeight)
            {
                return null;
            }

            var block = await _blockchainService.GetBlockByHashAsync(blockHeader.GetHash());

            if (!_bloom.IsIn(new Bloom(block.Header.Bloom.ToByteArray())))
            {
                // No interested event in the block
                return null;
            }

            Logger.LogTrace("ChainPrimaryTokenSymbolSet event received.");

            foreach (var transactionId in block.Body.TransactionIds)
            {
                var result = await _transactionResultQueryService.GetTransactionResultAsync(transactionId);
                if (result == null)
                {
                    Logger.LogTrace($"Transaction result is null, transactionId: {transactionId}");
                    continue;
                }

                if (result.Status == TransactionResultStatus.Failed)
                {
                    Logger.LogTrace(
                        $"Transaction failed, transactionId: {transactionId}, error: {result.Error}");
                    continue;
                }

                if (result.Bloom.Length == 0 || !_bloom.IsIn(new Bloom(result.Bloom.ToByteArray())))
                {
                    continue;
                }

                foreach (var log in result.Logs)
                {
                    if (log.Address != _contractAddress || log.Name != _logEvent.Name)
                        continue;

                    var message = new ChainPrimaryTokenSymbolSet();
                    message.MergeFrom(log);

                    Logger.LogTrace($"Chain primary token symbol: {message.TokenSymbol}");

                    return message.TokenSymbol;
                }
            }

            return null;
        }
    }
}