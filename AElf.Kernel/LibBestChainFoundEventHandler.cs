using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Consensus;
using AElf.Kernel.SmartContract.Application;
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
        private readonly ByteString _libTopicFlag = ByteString.CopyFrom(Hash.FromString("LIBFound").DumpByteArray());
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITransactionResultQueryService _transactionResultQueryService;

        public LibBestChainFoundEventHandler(IBlockchainService blockchainService,
            ITransactionResultQueryService transactionResultQueryService,
            ISmartContractAddressService smartContractAddressService)
        {
            _blockchainService = blockchainService;
            _transactionResultQueryService = transactionResultQueryService;
            _smartContractAddressService = smartContractAddressService;
            LocalEventBus = NullLocalEventBus.Instance;

            Logger = NullLogger<LibBestChainFoundEventHandler>.Instance;
        }

        public ILogger<LibBestChainFoundEventHandler> Logger { get; set; }
        public ILocalEventBus LocalEventBus { get; set; }

        public async Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            if (eventData.ExecutedBlocks == null) return;

            foreach (var executedBlock in eventData.ExecutedBlocks)
            {
                Logger.LogTrace($"Check event for block {executedBlock}");

                var block = await _blockchainService.GetBlockByHashAsync(executedBlock);
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

                    foreach (var contractEvent in result.Logs)
                    {
                        var address =
                            _smartContractAddressService.GetAddressByContractName(
                                ConsensusSmartContractAddressNameProvider.Name);
                        if (contractEvent.Address != address || !contractEvent.Topics.Contains(_libTopicFlag))
                            continue;

                        var offset = ExtractLibOffset(contractEvent);
                        var libHeight = block.Height - offset;

                        var chain = await _blockchainService.GetChainAsync();
                        var blockHash =
                            await _blockchainService.GetBlockHashByHeightAsync(chain, libHeight, chain.BestChainHash);

                        Logger.LogInformation($"Lib setting, block: {block}, tx: {transactionHash}, offset: {offset}");
                        await _blockchainService.SetIrreversibleBlockAsync(chain, libHeight, blockHash);
                    }
                }
            }
        }

        // TODO: Reimplement this if we can remove Unpack method.
        private long ExtractLibOffset(LogEvent logEvent)
        {
            return (long) ParamsPacker.Unpack(logEvent.Data.ToByteArray(), new[] {typeof(long)})[0];
        }
    }
}