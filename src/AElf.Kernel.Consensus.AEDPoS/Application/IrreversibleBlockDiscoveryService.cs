using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public interface IIrreversibleBlockDiscoveryService
    {
        Task<IBlockIndex> DiscoverAndSetIrreversibleAsync(Chain chain, IEnumerable<Hash> blockIdsInOrder);
    }

    public class IrreversibleBlockDiscoveryService : IIrreversibleBlockDiscoveryService, ITransientDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionResultQueryService _transactionResultQueryService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        public ILogger<IrreversibleBlockDiscoveryService> Logger { get; set; }
        public ILocalEventBus LocalEventBus { get; set; }
        private Address _contractAddress;
        private IrreversibleBlockFound _interestedEvent;
        private LogEvent _logEvent;
        private Bloom _bloom;

        public IrreversibleBlockDiscoveryService(IBlockchainService blockchainService,
            ITransactionResultQueryService transactionResultQueryService,
            ISmartContractAddressService smartContractAddressService)
        {
            _blockchainService = blockchainService;
            _transactionResultQueryService = transactionResultQueryService;
            _smartContractAddressService = smartContractAddressService;
            LocalEventBus = NullLocalEventBus.Instance;
            Logger = NullLogger<IrreversibleBlockDiscoveryService>.Instance;
        }

        private void PrepareBloom()
        {
            if (_bloom != null)
            {
                // already prepared
                return;
            }

            _contractAddress =
                _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);
            _interestedEvent = new IrreversibleBlockFound();
            _logEvent = _interestedEvent.ToLogEvent(_contractAddress);
            _bloom = _logEvent.GetBloom();
        }

        public async Task<IBlockIndex> DiscoverAndSetIrreversibleAsync(Chain chain, IEnumerable<Hash> blockIdsInOrder)
        {
            PrepareBloom();

            var reverse = blockIdsInOrder.Reverse();

            foreach (var blockId in reverse)
            {
                var block = await _blockchainService.GetBlockByHashAsync(blockId);
                Logger.LogTrace($"Check event for block {blockId} - {block.Height}");

                if (!_bloom.IsIn(new Bloom(block.Header.Bloom.ToByteArray())))
                {
                    // No interested event in the block
                    continue;
                }

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

                        var message = new IrreversibleBlockFound();
                        message.MergeFrom(log);

                        //var offset = message.Offset;
                        //var libHeight = block.Height - offset;

                        var libHeight = message.IrreversibleBlockHeight;

                        if (chain.LastIrreversibleBlockHeight >= libHeight)
                            return null;

                        var libBlock = await _blockchainService.GetBlockHashByHeightAsync(chain, libHeight, blockId);

                        return new BlockIndex(libBlock, libHeight);
                    }
                }
            }

            return null;
        }
    }
}