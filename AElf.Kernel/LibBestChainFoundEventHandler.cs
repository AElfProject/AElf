using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Infrastructure;
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
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;

        public ILogger<LibBestChainFoundEventHandler> Logger { get; set; }

        public ILocalEventBus LocalEventBus { get; set; }

        public LibBestChainFoundEventHandler(IBlockchainService blockchainService, IBlockManager blockManager,
            ITransactionResultQueryService transactionResultQueryService, IChainManager chainManager)
        {
            _blockchainService = blockchainService;
            _blockManager = blockManager;
            _transactionResultQueryService = transactionResultQueryService;
            _chainManager = chainManager;
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
                            _defaultContractZeroCodeProvider.ContractZeroAddress &&
                            contractEvent.Topics.Contains(
                                ByteString.CopyFrom(Hash.FromString("LIBFound").DumpByteArray())))
                        {
                            var indexingEventData = ExtractLibFoundData(contractEvent);
                            var offset = (long) indexingEventData[0];
                            var libHeight = eventData.BlockHeight - offset;
                            var chain = await _blockchainService.GetChainAsync();
                            var libHash = await _blockchainService.GetBlockHashByHeightAsync(chain, libHeight);

                            await _blockchainService.SetIrreversibleBlockAsync(chain, libHeight, libHash);
                            Logger.LogInformation("Lib setting finished.");
                        }
                    }
                }
            }
        }


        private object[] ExtractLibFoundData(LogEvent logEvent)
        {
            return ParamsPacker.Unpack(logEvent.Data.ToByteArray(), new[] {typeof(long)});
        }
    }
}