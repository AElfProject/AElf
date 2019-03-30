using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.Consensus.DPoS.Application
{
    public interface IIrreversibleBlockDiscoveryService
    {
        Task DiscoverAndSetIrreversibleAsync(IEnumerable<Hash> blockIds);
    }

    public class IrreversibleBlockDiscoveryService : IIrreversibleBlockDiscoveryService
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

        public async Task DiscoverAndSetIrreversibleAsync(IEnumerable<Hash> blockIds)
        {
            PrepareBloom();
            var heights = await DiscoverIrreversibleHeights(blockIds);
            await SetIrreversibleAsync(heights);
        }

        private async Task<IEnumerable<long>> DiscoverIrreversibleHeights(IEnumerable<Hash> blockIds)
        {
            //TODO: do not need check in order 
            //BODY: 1,2,3..... should check 5,4,3,2...., if 4 is LIB, set 2,3,4 as LIB 
            
            var output = new List<long>();
            foreach (var blockId in blockIds)
            {
                Logger.LogTrace($"Check event for block {blockId}");

                var block = await _blockchainService.GetBlockByHashAsync(blockId);
                if (!_bloom.IsIn(new Bloom(block.Header.Bloom.ToByteArray())))
                {
                    // No interested event in the block
                    continue;
                }

                foreach (var transactionHash in block.Body.Transactions)
                {
                    var result = await _transactionResultQueryService.GetTransactionResultAsync(transactionHash);
                    if (result == null)
                    {
                        Logger.LogTrace($"Transaction result is null, transactionHash: {transactionHash}");
                        continue;
                    }

                    if (result.Status == TransactionResultStatus.Failed)
                    {
                        Logger.LogTrace(
                            $"Transaction failed, transactionHash: {transactionHash}, error: {result.Error}");
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

                        var offset = message.Offset;
                        output.Add(block.Height - offset);
                    }
                }
            }

            return output;
        }

        private async Task SetIrreversibleAsync(IEnumerable<long> heights)
        {
            var chain = await _blockchainService.GetChainAsync();
            foreach (var height in heights)
            {
                var blockHash = await _blockchainService.GetBlockHashByHeightAsync(chain, height, chain.BestChainHash);

                await _blockchainService.SetIrreversibleBlockAsync(chain, height, blockHash);
            }
        }
    }
}