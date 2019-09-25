using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CSharp.Core;
using AElf.Kernel.Blockchain.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.Application
{
    public class ContractEventDiscoveryService<T> where T : IEvent<T>, new()
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionResultQueryService _transactionResultQueryService;
        public ILogger<ContractEventDiscoveryService<T>> Logger { get; set; }
        public ILocalEventBus LocalEventBus { get; set; }

        private LogEvent _logEvent;
        private Bloom _bloom;

        public ContractEventDiscoveryService(IBlockchainService blockchainService,
            ITransactionResultQueryService transactionResultQueryService)
        {
            _blockchainService = blockchainService;
            _transactionResultQueryService = transactionResultQueryService;
            LocalEventBus = NullLocalEventBus.Instance;
            Logger = NullLogger<ContractEventDiscoveryService<T>>.Instance;
        }

        public async Task<IEnumerable<T>> GetEventMessagesAsync(Hash blockHash, Address contractAddress = null)
        {
            PrepareBloom(contractAddress);
            var block = await _blockchainService.GetBlockByHashAsync(blockHash);

            var messages = new List<T>();

            if (!_bloom.IsIn(new Bloom(block.Header.Bloom.ToByteArray()))) return messages;

            foreach (var transactionId in block.Body.TransactionIds)
            {
                var transactionExecutingResult =
                    await _transactionResultQueryService.GetTransactionResultAsync(transactionId);
                if (transactionExecutingResult == null)
                {
                    Logger.LogTrace($"Transaction result is null, transactionId: {transactionId}");
                    continue;
                }

                if (transactionExecutingResult.Status == TransactionResultStatus.Failed)
                {
                    Logger.LogTrace(
                        $"Transaction failed, transactionId: {transactionId}, error: {transactionExecutingResult.Error}");
                    continue;
                }

                if (transactionExecutingResult.Bloom.Length == 0 ||
                    !_bloom.IsIn(new Bloom(transactionExecutingResult.Bloom.ToByteArray())))
                {
                    continue;
                }

                foreach (var log in transactionExecutingResult.Logs)
                {
                    if (log.Address != contractAddress || log.Name != _logEvent.Name)
                        continue;

                    var message = new T();
                    message.MergeFrom(log);
                    messages.Add(message);
                }
            }

            return messages;
        }

        private void PrepareBloom(Address contractAddress)
        {
            if (_bloom != null)
            {
                // already prepared
                return;
            }

            _logEvent = new T().ToLogEvent(contractAddress);
            _bloom = _logEvent.GetBloom();
        }
    }
}