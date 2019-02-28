using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    class CurrentOperation
    {
        public static readonly CurrentOperation Nothing = new CurrentOperation("Nothing");
        public static readonly CurrentOperation SwitchingBestChain = new CurrentOperation("SwitchingBestChain");
        public static readonly CurrentOperation HandlingLIB = new CurrentOperation("HandlingLIB");
        public static readonly CurrentOperation AddingTransaction = new CurrentOperation("AddingTransaction");
        public static readonly CurrentOperation RetrievingTransactions = new CurrentOperation("RetrievingTransactions");
        public string Name { get; }

        private CurrentOperation(string name)
        {
            Name = name;
        }

        public override bool Equals(object other)
        {
            return Equals(other as CurrentOperation);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }

        public bool Equals(CurrentOperation other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return false;
        }
    }

    public class TxHub : ITxHub, ISingletonDependency
    {
        public ILogger<TxHub> Logger { get; set; }

        private readonly ITransactionManager _transactionManager;
        private readonly IBlockchainService _blockchainService;

        private readonly Dictionary<Hash, TransactionReceipt> _allTransactions =
            new Dictionary<Hash, TransactionReceipt>();

        private ConcurrentDictionary<Hash, TransactionReceipt> _queuedTransactions =
            new ConcurrentDictionary<Hash, TransactionReceipt>();

        private Dictionary<Hash, TransactionReceipt> _validated = new Dictionary<Hash, TransactionReceipt>();

        private Dictionary<ulong, Dictionary<Hash, TransactionReceipt>> _invalidatedByBlock =
            new Dictionary<ulong, Dictionary<Hash, TransactionReceipt>>();

        private Dictionary<ulong, Dictionary<Hash, TransactionReceipt>> _expiredByExpiryBlock =
            new Dictionary<ulong, Dictionary<Hash, TransactionReceipt>>();

        private Dictionary<ulong, Dictionary<Hash, TransactionReceipt>> _futureByBlock =
            new Dictionary<ulong, Dictionary<Hash, TransactionReceipt>>();

        private ulong _bestChainHeight = ChainConsts.GenesisBlockHeight - 1;
        private Hash _bestChainHash = Hash.Genesis;
        private ulong _libHeight = ChainConsts.GenesisBlockHeight - 1;
        private Hash _libHash = Hash.Genesis;
        private CurrentOperation _currentOperation = CurrentOperation.Nothing;
        public int ChainId { get; private set; }

        public TxHub(ITransactionManager transactionManager, IBlockchainService blockchainService)
        {
            Logger = NullLogger<TxHub>.Instance;
            _transactionManager = transactionManager;
            _blockchainService = blockchainService;
        }

        public async Task<bool> AddTransactionAsync(int chainId, Transaction transaction)
        {
            var receipt = new TransactionReceipt(transaction);

            if (_allTransactions.ContainsKey(receipt.TransactionId))
            {
                return false;
            }

            var txn = await _transactionManager.GetTransaction(receipt.TransactionId);
            if (txn != null)
            {
                return false;
            }

            _allTransactions.Add(receipt.TransactionId, receipt);
            await _transactionManager.AddTransactionAsync(transaction);
            var currentOperation = Interlocked.CompareExchange(ref _currentOperation,
                CurrentOperation.AddingTransaction, CurrentOperation.Nothing);
            if (currentOperation != null && currentOperation.Equals(CurrentOperation.Nothing))
            {
                // Successfully claimed, validate the transaction
                var prefix = await GetPrefixByHeightAsync(ChainId, receipt.Transaction.RefBlockNumber, _bestChainHash);
                CheckPrefixForOne(receipt, prefix, _bestChainHeight);
                AddToRespectiveCurrentCollection(receipt);
                Interlocked.CompareExchange(ref _currentOperation, CurrentOperation.Nothing,
                    CurrentOperation.AddingTransaction);
            }
            else
            {
                // Failed to claim, add the receipt to queue
                _queuedTransactions.TryAdd(receipt.TransactionId, receipt);
            }

            return true;
        }

        public async Task<ExecutableTransactionSet> GetExecutableTransactionSetAsync()
        {
            var chain = await _blockchainService.GetChainAsync(ChainId);
            if (chain.BestChainHash != _bestChainHash)
            {
                await HandleBestChainFoundAsync(
                    new BestChainFoundEvent()
                    {
                        ChainId = chain.Id,
                        BlockHash = chain.BestChainHash, BlockHeight = chain.BestChainHeight
                    });
            }

            var currentOperation = Interlocked.CompareExchange(ref _currentOperation,
                CurrentOperation.RetrievingTransactions, CurrentOperation.Nothing);
            if (currentOperation != null && currentOperation.Equals(CurrentOperation.Nothing))
            {
                var output = new ExecutableTransactionSet()
                {
                    ChainId = ChainId,
                    PreviousBlockHash = _bestChainHash,
                    PreviousBlockHeight = _bestChainHeight
                };
                output.Transactions.AddRange(_validated.Values.Select(x => x.Transaction));
                // Successfully claimed
                Interlocked.CompareExchange(ref _currentOperation, CurrentOperation.Nothing,
                    CurrentOperation.RetrievingTransactions);
                return output;
            }

            // Maybe throw exception when failed to claim   
            return new ExecutableTransactionSet()
            {
                ChainId = ChainId,
                PreviousBlockHash = _bestChainHash,
                PreviousBlockHeight = _bestChainHeight
            };
        }

        public Task<TransactionReceipt> GetTransactionReceiptAsync(Hash transactionId)
        {
            _allTransactions.TryGetValue(transactionId, out var receipt);
            return Task.FromResult(receipt);
        }

        public void Dispose()
        {
        }

        public async Task<IDisposable> StartAsync(int chainId)
        {
            ChainId = chainId;
            return this;
        }

        public async Task StopAsync()
        {
        }


        #region Private Methods

        #region Private Static Methods

        private static void AddToCollection(Dictionary<ulong, Dictionary<Hash, TransactionReceipt>> collection,
            TransactionReceipt receipt)
        {
            if (!collection.TryGetValue(receipt.Transaction.RefBlockNumber, out var receipts))
            {
                receipts = new Dictionary<Hash, TransactionReceipt>();
                collection.Add(receipt.Transaction.RefBlockNumber, receipts);
            }

            receipts.Add(receipt.TransactionId, receipt);
        }

        private static void CheckPrefixForOne(TransactionReceipt receipt, ByteString prefix,
            ulong bestChainHeight)
        {
            if (receipt.Transaction.GetExpiryBlockNumber() <= bestChainHeight)
            {
                receipt.RefBlockStatus = RefBlockStatus.RefBlockExpired;
                return;
            }

            if (prefix == null)
            {
                receipt.RefBlockStatus = RefBlockStatus.FutureRefBlock;
                return;
            }

            if (receipt.Transaction.RefBlockPrefix == prefix)
            {
                receipt.RefBlockStatus = RefBlockStatus.RefBlockValid;
                return;
            }

            receipt.RefBlockStatus = RefBlockStatus.RefBlockInvalid;
        }

        #endregion

        private List<TransactionReceipt> GetRevalidationCandidates()
        {
            var invalidated = _invalidatedByBlock;
            _invalidatedByBlock = new Dictionary<ulong, Dictionary<Hash, TransactionReceipt>>();
            var expired = _expiredByExpiryBlock;
            _expiredByExpiryBlock = new Dictionary<ulong, Dictionary<Hash, TransactionReceipt>>();
            var future = _futureByBlock;
            var toRevalidate = new List<TransactionReceipt>();
            toRevalidate.AddRange(invalidated.Values.SelectMany(x => x.Values));
            toRevalidate.AddRange(expired.Values.SelectMany(x => x.Values));
            toRevalidate.AddRange(future.Values.SelectMany(x => x.Values));
            var queuedTransactions = Interlocked.Exchange(ref _queuedTransactions,
                new ConcurrentDictionary<Hash, TransactionReceipt>());
            toRevalidate.AddRange(queuedTransactions.Values);
            return toRevalidate;
        }

        private async Task<ByteString> GetPrefixByHeightAsync(Chain chain, ulong height, Hash bestChainHash)
        {
            var hash = await _blockchainService.GetBlockHashByHeightAsync(chain, height, bestChainHash);
            return hash == null ? null : ByteString.CopyFrom(hash.DumpByteArray().Take(4).ToArray());
        }

        private async Task<ByteString> GetPrefixByHeightAsync(int chainId, ulong height, Hash bestChainHash)
        {
            var chain = await _blockchainService.GetChainAsync(chainId);
            return await GetPrefixByHeightAsync(chain, height, bestChainHash);
        }

        private async Task<Dictionary<ulong, ByteString>> GetPrefixesByHeightAsync(int chainId,
            IEnumerable<ulong> heights, Hash bestChainHash)
        {
            var prefixes = new Dictionary<ulong, ByteString>();
            var chain = await _blockchainService.GetChainAsync(chainId);
            foreach (var h in heights)
            {
                var prefix = await GetPrefixByHeightAsync(chain, h, bestChainHash);
                prefixes.Add(h, prefix);
            }

            return prefixes;
        }

        private void ResetCurrentCollections()
        {
            _expiredByExpiryBlock = new Dictionary<ulong, Dictionary<Hash, TransactionReceipt>>();
            _invalidatedByBlock = new Dictionary<ulong, Dictionary<Hash, TransactionReceipt>>();
            _futureByBlock = new Dictionary<ulong, Dictionary<Hash, TransactionReceipt>>();
            _validated = new Dictionary<Hash, TransactionReceipt>();
        }

        private void AddToRespectiveCurrentCollection(TransactionReceipt receipt)
        {
            switch (receipt.RefBlockStatus)
            {
                case RefBlockStatus.RefBlockExpired:
                    AddToCollection(_expiredByExpiryBlock, receipt);
                    break;
                case RefBlockStatus.FutureRefBlock:
                    AddToCollection(_futureByBlock, receipt);
                    break;
                case RefBlockStatus.RefBlockInvalid:
                    AddToCollection(_invalidatedByBlock, receipt);
                    break;
                case RefBlockStatus.RefBlockValid:
                    _validated.Add(receipt.TransactionId, receipt);
                    break;
            }
        }

        /// <summary>
        /// Re-validates previously non-valid transactions (i.e. expired, invalid, future) together with the delta
        /// (i.e. executed in previous branch but not on current branch).
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="bestChainHash"></param>
        /// <param name="delta"></param>
        /// <returns></returns>
        private async Task RevalidateTransactions(int chainId, Hash bestChainHash, IEnumerable<Hash> delta)
        {
            var toRevalidate = GetRevalidationCandidates();
            toRevalidate.AddRange(delta.Select(x => _allTransactions[x]));
            var heights = toRevalidate.Select(x => x.Transaction.RefBlockNumber).Distinct();
            var prefixes = await GetPrefixesByHeightAsync(chainId, heights, bestChainHash);
            ResetCurrentCollections();
            foreach (var r in toRevalidate)
            {
                var prefix = prefixes[r.Transaction.RefBlockNumber];
                CheckPrefixForOne(r, prefix, _bestChainHeight);
                AddToRespectiveCurrentCollection(r);
            }
        }

        #endregion

        #region Event Handler Methods

        public async Task HandleBestChainFoundAsync(BestChainFoundEvent eventData)
        {
            if (ChainId != eventData.ChainId)
            {
                return;
            }

            var currentOperation = Interlocked.CompareExchange(ref _currentOperation,
                CurrentOperation.SwitchingBestChain, CurrentOperation.Nothing);
            if (currentOperation != null && currentOperation.Equals(CurrentOperation.Nothing))
            {
                // successfully claimed
                var branchSwitch =
                    await _blockchainService.GetBranchSwitchAsync(eventData.ChainId, _bestChainHash,
                        eventData.BlockHash);
                var notExecuted = new HashSet<Hash>();
                foreach (var rb in branchSwitch.RollBack)
                {
                    var block = await _blockchainService.GetBlockByHashAsync(eventData.ChainId, rb);
                    foreach (var txId in block.Body.Transactions)
                    {
                        if (_allTransactions.TryGetValue(txId, out var receipt))
                        {
                            receipt.ExecutedBlockNumber = 0;
                            notExecuted.Add(txId);
                        }
                    }
                }

                var executed = new HashSet<Hash>();
                foreach (var rf in branchSwitch.RollForward)
                {
                    var block = await _blockchainService.GetBlockByHashAsync(eventData.ChainId, rf);
                    foreach (var txId in block.Body.Transactions)
                    {
                        if (_allTransactions.TryGetValue(txId, out var receipt))
                        {
                            receipt.ExecutedBlockNumber = block.Height;
                            executed.Add(txId);
                        }
                    }
                }

                notExecuted.ExceptWith(executed);

                await RevalidateTransactions(eventData.ChainId, eventData.BlockHash, notExecuted);
                _bestChainHash = eventData.BlockHash;
                _bestChainHeight = eventData.BlockHeight;
                Interlocked.CompareExchange(ref _currentOperation, CurrentOperation.Nothing,
                    CurrentOperation.SwitchingBestChain);
            }
        }

        public async Task HandleNewIrreversibleBlockFoundAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            if (ChainId != eventData.ChainId)
            {
                return;
            }

            var currentOperation = Interlocked.CompareExchange(ref _currentOperation,
                CurrentOperation.HandlingLIB, CurrentOperation.Nothing);
            if (currentOperation != null && currentOperation.Equals(CurrentOperation.Nothing))
            {
                // Remove all executed transactions on and before lib
                var hash = eventData.BlockHash;
                while (hash != _libHash)
                {
                    var block = await _blockchainService.GetBlockByHashAsync(eventData.ChainId, hash);
                    foreach (var txId in block.Body.Transactions)
                    {
                        _allTransactions.Remove(txId);
                    }

                    hash = block.Header.PreviousBlockHash;
                }

                // Remove all expired transactions on and before lib
                var height = eventData.BlockHeight;
                var expiredToRemove = _expiredByExpiryBlock.Keys.Where(x => x <= height).ToList();
                foreach (var r in expiredToRemove)
                {
                    _expiredByExpiryBlock.Remove(r);
                }

                // Update lib record
                _libHash = eventData.BlockHash;
                _libHeight = eventData.BlockHeight;
                Interlocked.CompareExchange(ref _currentOperation, CurrentOperation.Nothing,
                    CurrentOperation.HandlingLIB);
            }
        }

        #endregion
    }
}