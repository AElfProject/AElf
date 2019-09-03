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
    public interface IIrreversibleBlockRelatedEventsDiscoveryService
    {
        Task<IBlockIndex> GetLastIrreversibleBlockIndexAsync(Chain chain, IEnumerable<Hash> blockHashesInOrder);
        Task<long> GetUnacceptableDistanceToLastIrreversibleBlockHeightAsync(Hash blockHash);
    }

    public class IrreversibleBlockRelatedEventsDiscoveryService : IIrreversibleBlockRelatedEventsDiscoveryService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionResultQueryService _transactionResultQueryService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        public ILogger<IrreversibleBlockRelatedEventsDiscoveryService> Logger { get; set; }
        public ILocalEventBus LocalEventBus { get; set; }
        private Address _contractAddress;

        private LogEvent _logEventOfLibFound;
        private Bloom _bloomOfLibFound;

        private LogEvent _logEventOfLibUnacceptable;
        private Bloom _bloomOfLibUnacceptable;

        public IrreversibleBlockRelatedEventsDiscoveryService(IBlockchainService blockchainService,
            ITransactionResultQueryService transactionResultQueryService,
            ISmartContractAddressService smartContractAddressService)
        {
            _blockchainService = blockchainService;
            _transactionResultQueryService = transactionResultQueryService;
            _smartContractAddressService = smartContractAddressService;
            LocalEventBus = NullLocalEventBus.Instance;
            Logger = NullLogger<IrreversibleBlockRelatedEventsDiscoveryService>.Instance;
        }

        private void PrepareBloomForIrreversibleBlockFound()
        {
            if (_bloomOfLibFound != null)
            {
                // already prepared
                return;
            }

            _contractAddress =
                _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);
            _logEventOfLibFound = new IrreversibleBlockFound().ToLogEvent(_contractAddress);
            _bloomOfLibFound = _logEventOfLibFound.GetBloom();
        }

        private void PrepareBloomForIrreversibleBlockHeightUnacceptable()
        {
            if (_bloomOfLibUnacceptable != null)
            {
                // already prepared
                return;
            }

            _contractAddress =
                _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);
            _logEventOfLibUnacceptable = new IrreversibleBlockHeightUnacceptable().ToLogEvent(_contractAddress);
            _bloomOfLibUnacceptable = _logEventOfLibUnacceptable.GetBloom();
        }

        public async Task<long> GetUnacceptableDistanceToLastIrreversibleBlockHeightAsync(Hash blockHash)
        {
            PrepareBloomForIrreversibleBlockHeightUnacceptable();

            var block = await _blockchainService.GetBlockByHashAsync(blockHash);

            if (_bloomOfLibUnacceptable.IsIn(new Bloom(block.Header.Bloom.ToByteArray())))
            {
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

                    if (result.Bloom.Length == 0 || !_bloomOfLibUnacceptable.IsIn(new Bloom(result.Bloom.ToByteArray())))
                    {
                        continue;
                    }

                    foreach (var log in result.Logs)
                    {
                        if (log.Address != _contractAddress || log.Name != _logEventOfLibUnacceptable.Name)
                            continue;

                        var message = new IrreversibleBlockHeightUnacceptable();
                        message.MergeFrom(log);
                        Logger.LogTrace(
                            $"IrreversibleBlockHeightUnacceptable detected: {message}");
                        return message.DistanceToIrreversibleBlockHeight;
                    }
                }
            }

            return 0;
        }

        public async Task<IBlockIndex> GetLastIrreversibleBlockIndexAsync(Chain chain,
            IEnumerable<Hash> blockHashesInOrder)
        {
            PrepareBloomForIrreversibleBlockFound();

            var reverse = blockHashesInOrder.Reverse();

            foreach (var blockId in reverse)
            {
                var block = await _blockchainService.GetBlockByHashAsync(blockId);
                Logger.LogTrace($"Check event for block {blockId} - {block.Height}");

                if (!_bloomOfLibFound.IsIn(new Bloom(block.Header.Bloom.ToByteArray())))
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

                    if (result.Bloom.Length == 0 || !_bloomOfLibFound.IsIn(new Bloom(result.Bloom.ToByteArray())))
                    {
                        continue;
                    }

                    foreach (var log in result.Logs)
                    {
                        if (log.Address != _contractAddress || log.Name != _logEventOfLibFound.Name)
                            continue;

                        var message = new IrreversibleBlockFound();
                        message.MergeFrom(log);

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