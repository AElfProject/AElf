using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.EventMessages;
using AElf.Kernel.TransactionPool.Domain;
using AElf.Kernel.TransactionPool.RefBlockExceptions;
using AElf.Kernel.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public class TxHub : ITxHub, ISingletonDependency 
    {
        public ILogger<TxHub> Logger {get;set;}
        
        private readonly ITransactionManager _transactionManager;
        private readonly ITransactionReceiptManager _receiptManager;
        private readonly ITxRefBlockValidator _refBlockValidator;
        
        private readonly ConcurrentDictionary<Hash, TransactionReceipt> _allTxns =
            new ConcurrentDictionary<Hash, TransactionReceipt>();
                
        private ulong _curHeight;
        

        public TxHub(ITransactionManager transactionManager, ITransactionReceiptManager receiptManager,
            ITxRefBlockValidator refBlockValidator)
        {
            Logger = NullLogger<TxHub>.Instance;
            _transactionManager = transactionManager;
            _receiptManager = receiptManager;
            _refBlockValidator = refBlockValidator;
            
        }

        public void Initialize(int chainId)
        {
            
        }

        public void Start()
        {
        }

        public Task Stop()
        {
            return Task.CompletedTask;
        }

        public async Task AddTransactionAsync(int chainId, Transaction transaction, bool skipValidation = false)
        {
            var tr = new TransactionReceipt(transaction);
            if (skipValidation)
            {
                tr.SignatureStatus = SignatureStatus.SignatureValid;
                tr.RefBlockStatus = RefBlockStatus.RefBlockValid;
            }

            var txn = await _transactionManager.GetTransaction(tr.TransactionId);

            // if the transaction is in TransactionManager, it is either executed or added into _allTxns
            if (txn != null && !txn.Equals(new Transaction()))
            {
                // Logger.LogWarning($"Transaction {transaction.GetHash()} already exists.");
                return;
            }

            if (!_allTxns.TryAdd(tr.TransactionId, tr))
            {
                // Logger.LogWarning($"Transaction {transaction.GetHash()} already exists.");
                return;
            }

            IdentifyTransactionType(chainId, tr);

            // todo this should be up to the caller of this method to choose
            // todo weither or not this is done on another thread, currently 
            // todo this gives the caller no choice.

            var task = Task.Run(async () =>
            {
                await VerifySignature(chainId, tr);
                await ValidateRefBlock(chainId, tr);
                MaybePublishTransaction(tr);
            });
        }

        public async Task<List<TransactionReceipt>> GetReceiptsOfExecutablesAsync()
        {
            return await Task.FromResult(_allTxns.Values.Where(x => x.IsExecutable).ToList());
        }

        public async Task<TransactionReceipt> GetCheckedReceiptsAsync(int chainId, Transaction txn)
        {
            if (!_allTxns.TryGetValue(txn.GetHash(), out var tr))
            {
                tr = new TransactionReceipt(txn);
                _allTxns.TryAdd(tr.TransactionId, tr);
            }
            await VerifySignature(chainId, tr);
            await ValidateRefBlock(chainId, tr);
            return tr;
        }

        public async Task<TransactionReceipt> GetReceiptAsync(Hash txId)
        {
            if (!_allTxns.TryGetValue(txId, out var tr))
            {
                tr = await _receiptManager.GetReceiptAsync(txId);
            }

            return tr;
        }

        public async Task<Transaction> GetTxAsync(Hash txId)
        {
            var tr = await GetReceiptAsync(txId);
            if (tr != null)
            {
                return tr.Transaction;
            }

            return await Task.FromResult<Transaction>(null);
        }

        public bool TryGetTx(Hash txHash, out Transaction tx)
        {
            tx = GetTxAsync(txHash).Result;
            return tx != null;
        }

        private static void MaybePublishTransaction(TransactionReceipt tr)
        {
            if (tr.Transaction.ShouldNotBroadcast())
            {
                return;
            }
            
            if (tr.IsExecutable && tr.ToBeBroadCasted)
            {
                MessageHub.Instance.Publish(new TransactionAddedToPool(tr.Transaction));
            }
        }

        #region Private Methods

        private async Task VerifySignature(int chainId, TransactionReceipt tr)
        {
            if (tr.SignatureStatus != SignatureStatus.UnknownSignatureStatus)
            {
                return;
            }

            if(tr.Transaction.Sigs.Count > 1)
            {
                if (tr.Transaction.From.Equals(Address.Genesis))
                {
                    // validate miners authorization
                    var authorizationResult = await ValidateMinersAuthorization(chainId, tr);
                    if (!authorizationResult)
                    {
                        tr.SignatureStatus = SignatureStatus.SignatureInvalid;
                        return;
                    }
                }
                else
                {
                    // validate authorization for multi-sig address
                    var validAuthorization = await CheckAuthority(chainId, tr.Transaction);
                    if (!validAuthorization)
                    {
                        tr.SignatureStatus = SignatureStatus.SignatureInvalid;
                        return;
                    }
                }
                tr.SignatureStatus = SignatureStatus.SignatureValid;
                return;
            }

            var validSig = tr.Transaction.VerifySignature();
            tr.SignatureStatus = validSig
                ? SignatureStatus.SignatureValid
                : SignatureStatus.SignatureInvalid;
        }



        
        
        private async Task ValidateRefBlock(int chainId, TransactionReceipt tr)
        {
            if (tr.RefBlockStatus != RefBlockStatus.UnknownRefBlockStatus &&
                tr.RefBlockStatus != RefBlockStatus.FutureRefBlock)
            {
                return;
            }

            try
            {
                await _refBlockValidator.ValidateAsync(chainId, tr.Transaction);
                tr.RefBlockStatus = RefBlockStatus.RefBlockValid;
            }
            catch (FutureRefBlockException)
            {
                tr.RefBlockStatus = RefBlockStatus.FutureRefBlock;
            }
            catch (RefBlockInvalidException)
            {
                tr.RefBlockStatus = RefBlockStatus.RefBlockInvalid;
            }
            catch (RefBlockExpiredException)
            {
                tr.RefBlockStatus = RefBlockStatus.RefBlockExpired;
            }
        }

        private void IdentifyTransactionType(int chainId, TransactionReceipt tr)
        {
            if (SystemAddresses.Contains(tr.Transaction.To))
            {
                tr.IsSystemTxn = true;
            }
        }

        #endregion Private Methods

        #region Event Handlers

        // Change transaction status and add transaction into TransactionManager.
        private void UpdateExecutedTransactions(IEnumerable<Hash> txIds, ulong blockNumber)
        {
            var receipts = new List<TransactionReceipt>();
            foreach (var txId in txIds)
            {
                if (_allTxns.TryGetValue(txId, out var tr))
                {
                    tr.TransactionStatus = TransactionStatus.TransactionExecuted;
                    tr.ExecutedBlockNumber = blockNumber;
                    _transactionManager.AddTransactionAsync(tr.Transaction);
                    receipts.Add(tr);
                }
                else
                {
                }
            }

            _receiptManager.AddOrUpdateReceiptsAsync(receipts);
        }

        private void IdentifyExpiredTransactions()
        {
            if (_curHeight > GlobalConfig.ReferenceBlockValidPeriod)
            {
                var expired = _allTxns.Where(tr =>
                    tr.Value.TransactionStatus == TransactionStatus.UnknownTransactionStatus
                    && tr.Value.RefBlockStatus != RefBlockStatus.RefBlockExpired
                    && _curHeight > tr.Value.Transaction.RefBlockNumber
                    && _curHeight - tr.Value.Transaction.RefBlockNumber > GlobalConfig.ReferenceBlockValidPeriod
                );
                foreach (var tr in expired)
                {
                    tr.Value.RefBlockStatus = RefBlockStatus.RefBlockExpired;
                }
            }
        }

        private void RemoveOldTransactions()
        {
            // TODO: Improve
            // Remove old transactions (executed, invalid and expired)
            var keepNBlocks = GlobalConfig.ReferenceBlockValidPeriod / 4 * 5;
            if (_curHeight - GlobalConfig.GenesisBlockHeight > keepNBlocks)
            {
                var blockNumberThreshold = _curHeight - keepNBlocks;
                var toRemove = _allTxns.Where(tr => tr.Value.Transaction.RefBlockNumber < blockNumberThreshold);
                foreach (var tr in toRemove)
                {
                    _allTxns.TryRemove(tr.Key, out _);
                }
            }
        }

        private async Task RevalidateFutureTransactions(int chainId)
        {
            // Re-validate FutureRefBlock transactions
            foreach (var tr in _allTxns.Values.Where(x =>
                x.RefBlockStatus == RefBlockStatus.FutureRefBlock))
            {
                await ValidateRefBlock(chainId, tr);
            }
        }

        // Render transactions to expire, and purge old transactions (RefBlockValidPeriod + some buffer)
        public async Task OnNewBlock(Block block)
        {
            var blockHeader = block.Header;
            // TODO: Handle LIB
            if (blockHeader.Height > (_curHeight + 1) && _curHeight != GlobalConfig.GenesisBlockHeight)
            {
                throw new Exception($"Invalid block index {blockHeader.Height} but current height is {_curHeight}.");
            }

            _curHeight = blockHeader.Height;

            UpdateExecutedTransactions(block.Body.Transactions, block.Header.Height);

            IdentifyExpiredTransactions();

            RemoveOldTransactions();

            await RevalidateFutureTransactions(block.Header.ChainId);
        }


        #endregion
    }
}