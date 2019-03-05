using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContractExecution.Domain;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel
{
    // ReSharper disable InconsistentNaming
    public class LibBestChainFoundEventHandler : ILocalEventHandler<BestChainFoundEventData>, ITransientDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockManager _blockManager;
        private readonly ITransactionResultQueryService _transactionResultQueryService;
        private readonly IChainManager _chainManager;
        private readonly IBlockchainStateManager _blockchainStateManager;

        public ILogger<LibBestChainFoundEventHandler> Logger { get; set; }

        public ILocalEventBus LocalEventBus { get; set; }

        public LibBestChainFoundEventHandler(IBlockchainService blockchainService, IBlockManager blockManager,
            ITransactionResultQueryService transactionResultQueryService, IChainManager chainManager,
            IBlockchainStateManager blockchainStateManager)
        {
            _blockchainService = blockchainService;
            _blockManager = blockManager;
            _transactionResultQueryService = transactionResultQueryService;
            _chainManager = chainManager;
            _blockchainStateManager = blockchainStateManager;
            LocalEventBus = NullLocalEventBus.Instance;

            Logger = NullLogger<LibBestChainFoundEventHandler>.Instance;
        }


        public async Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            if (eventData.ExecutedBlocks == null)
            {
                return;
            }

            foreach (var executedBlock in eventData.ExecutedBlocks)
            {
                var block = await _blockManager.GetBlockAsync(executedBlock);

                foreach (var transactionHash in block.Body.Transactions)
                {
                    var result = await _transactionResultQueryService.GetTransactionResultAsync(transactionHash);
                    foreach (var contractEvent in result.Logs)
                    {
                        if (contractEvent.Address ==
                            _chainManager.GetConsensusContractAddress() &&
                            contractEvent.Topics.Contains(
                                ByteString.CopyFrom(Hash.FromString("LIBFound").DumpByteArray())))
                        {
                            var indexingEventData = ExtractLibFoundData(contractEvent);
                            var offset = (ulong) indexingEventData[0];
                            var libHeight = eventData.BlockHeight - offset;

                            var chain = await _blockchainService.GetChainAsync();
                            var libHash = await _blockchainService.GetBlockHashByHeightAsync(chain, libHeight);

                            Logger.LogInformation($"Lib height: {libHeight}, Lib Hash: {libHash}");

                            var chainStateInfo =
                                await _blockchainStateManager.GetChainStateInfoAsync();

                            var count = (int) libHeight - (int) chain.LastIrreversibleBlockHeight - 1;
                            var hashes =
                                await _blockchainService.GetReversedBlockHashes(libHash, count);

                            hashes.Reverse();

                            hashes.Add(libHash);

                            var startHeight = chain.LastIrreversibleBlockHeight + 1;
                            foreach (var hash in hashes)
                            {
                                try
                                {
                                    Logger.LogInformation($"Merge Lib hash: {hash}， height: {startHeight++}");
                                    await _blockchainStateManager.MergeBlockStateAsync(chainStateInfo, hash);
                                }
                                catch (Exception e)
                                {
                                    Logger.LogError(e.Message);
                                }
                            }

                            await _chainManager.SetIrreversibleBlockAsync(chain, libHash);

                            Logger.LogInformation("Lib setting finished.");
                        }
                    }
                }
            }
        }


        private object[] ExtractLibFoundData(LogEvent logEvent)
        {
            return ParamsPacker.Unpack(logEvent.Data.ToByteArray(), new[] {typeof(ulong)});
        }
    }
}