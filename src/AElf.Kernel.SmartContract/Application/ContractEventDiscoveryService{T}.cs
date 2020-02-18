using System;
using System.Collections.Generic;
using System.Linq;
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
    //TODO: is this class still in use?
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

        public async Task<IEnumerable<T>> GetEventMessagesAsync(Hash blockHash, Address contractAddress)
        {
            try
            {
                PrepareBloom(contractAddress);
                var block = await _blockchainService.GetBlockByHashAsync(blockHash);

                var messages = new List<T>();

                if (!_bloom.IsIn(new Bloom(block.Header.Bloom.ToByteArray()))) return messages;

                foreach (var transactionId in block.Body.TransactionIds)
                {
                    var transactionExecutingResult =
                        await _transactionResultQueryService.GetTransactionResultAsync(transactionId);
                    if (!CheckTransactionExecutingResult(transactionExecutingResult, transactionId))
                    {
                        continue;
                    }

                    foreach (var log in transactionExecutingResult.Logs)
                    {
                        if (contractAddress == null || log.Address != contractAddress || log.Name != _logEvent.Name)
                            continue;

                        var message = new T();
                        try
                        {
                            message.MergeFrom(log);
                            messages.Add(message);
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(e, $"Failed to generate message of type {message.GetType().FullName}.");
                            throw;
                        }
                    }
                }

                Logger.LogTrace($"Event of type {typeof(T).FullName} found.");

                return messages;
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Failed to resolve event {typeof(T).FullName}");
                throw;
            }
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

        private bool CheckTransactionExecutingResult(TransactionResult transactionExecutingResult, Hash transactionId)
        {
            if (transactionExecutingResult == null)
            {
                Logger.LogWarning($"Transaction result is null, transactionId: {transactionId}");
                return false;
            }

            if (transactionExecutingResult.Status == TransactionResultStatus.Failed)
            {
                Logger.LogWarning(
                    $"Transaction failed, transactionId: {transactionId}, error: {transactionExecutingResult.Error}");
                return false;
            }

            if (transactionExecutingResult.Bloom.Length == 0 ||
                !_bloom.IsIn(new Bloom(transactionExecutingResult.Bloom.ToByteArray())))
            {
                return false;
            }

            return true;
        }
    }
}