using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common;
using AElf.Common.Attributes;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.EventMessages;
using AElf.Kernel.Managers;
using AElf.Kernel.Types.Common;
using AElf.Kernel.Types.Transaction;
using AElf.Miner.EventMessages;
using AElf.Miner.TxMemPool.RefBlockExceptions;
using AElf.SmartContract.Proposal;
using Easy.MessageHub;
using NLog;

namespace AElf.Miner.TxMemPool
{
    [LoggerName(nameof(TxHub))]
    public class TxHub : ITxHub
    {
        private readonly ILogger _logger;
        
        private readonly ITransactionManager _transactionManager;
        private readonly ITransactionReceiptManager _receiptManager;
        private readonly ITxSignatureVerifier _signatureVerifier;
        private readonly ITxRefBlockValidator _refBlockValidator;
        private readonly IAuthorizationInfo _authorizationInfo;
        private readonly IChainService _chainService;
        
        private readonly ConcurrentDictionary<Hash, TransactionReceipt> _allTxns =
            new ConcurrentDictionary<Hash, TransactionReceipt>();
        
        private IBlockChain _blockChain;
        
        private static bool _terminated;

        private ulong _curHeight;

        private readonly Hash _chainId;

        private readonly Address _dPosContractAddress;
        private readonly Address _crossChainContractAddress;
        
        private List<Address> SystemAddresses => new List<Address>
        {
            _dPosContractAddress, 
            _crossChainContractAddress
        };

        public TxHub(ITransactionManager transactionManager, ITransactionReceiptManager receiptManager,
            IChainService chainService, IAuthorizationInfo authorizationInfo, ITxSignatureVerifier signatureVerifier,
            ITxRefBlockValidator refBlockValidator, ILogger logger)
        {
            _logger = logger;
            _transactionManager = transactionManager;
            _receiptManager = receiptManager;
            _chainService = chainService;
            _signatureVerifier = signatureVerifier;
            _refBlockValidator = refBlockValidator;
            _authorizationInfo = authorizationInfo;

            _terminated = false;

            _chainId = Hash.LoadBase58(ChainConfig.Instance.ChainId);

            _dPosContractAddress = ContractHelpers.GetConsensusContractAddress(_chainId);
            _crossChainContractAddress =   ContractHelpers.GetCrossChainContractAddress(_chainId);
        }

        public void Initialize()
        {
            _blockChain = _chainService.GetBlockChain(_chainId);

            if (_blockChain == null)
            {
                _logger?.Warn($"Could not find the blockchain for {_chainId}.");
                return;
            }

            _curHeight = _blockChain.GetCurrentBlockHeightAsync().Result;
                
            MessageHub.Instance.Subscribe<BranchRolledBack>(async branch =>
                await OnBranchRolledBack(branch.Blocks).ConfigureAwait(false));
            
            MessageHub.Instance.Subscribe<TerminationSignal>(signal =>
            {
                if (signal.Module == TerminatedModuleEnum.TxPool)
                {
                    _terminated = true;
                    MessageHub.Instance.Publish(new TerminatedModule(TerminatedModuleEnum.TxPool));
                }
            });
        }

        public void Start()
        {
        }

        public Task Stop()
        {
            return Task.CompletedTask;
        }

        public async Task AddTransactionAsync(Transaction transaction, bool skipValidation = false)
        {
            if (_terminated)
            {
                return;
            }

            var tr = new TransactionReceipt(transaction);
            if (skipValidation)
            {
                tr.SignatureSt = TransactionReceipt.Types.SignatureStatus.SignatureValid;
                tr.RefBlockSt = TransactionReceipt.Types.RefBlockStatus.RefBlockValid;
            }

            var txn = await _transactionManager.GetTransaction(tr.TransactionId);

            // if the transaction is in TransactionManager, it is either executed or added into _allTxns
            if (txn != null && !txn.Equals(new Transaction()))
            {
                // _logger?.Warn($"Transaction {transaction.GetHash()} already exists.");
                return;
            }

            if (!_allTxns.TryAdd(tr.TransactionId, tr))
            {
                // _logger?.Warn($"Transaction {transaction.GetHash()} already exists.");
                return;
            }

            IdentifyTransactionType(tr);

            // todo this should be up to the caller of this method to choose
            // todo weither or not this is done on another thread, currently 
            // todo this gives the caller no choice.
            
            Task.Run(async () =>
            {
                VerifySignature(tr);
                await ValidateRefBlock(tr);
                MaybePublishTransaction(tr);
            }).ConfigureAwait(false);
        }

        public async Task<List<TransactionReceipt>> GetReceiptsOfExecutablesAsync()
        {
            return await Task.FromResult(_allTxns.Values.Where(x => x.IsExecutable).ToList());
        }

        public async Task<List<TransactionReceipt>> GetReceiptsForAsync(IEnumerable<Transaction> transactions)
        {
            var trs = new List<TransactionReceipt>();
            // TODO: Check if parallelization is needed
            foreach (var txn in transactions)
            {
                if (!_allTxns.TryGetValue(txn.GetHash(), out var tr))
                {
                    tr = new TransactionReceipt(txn);
                    _allTxns.TryAdd(tr.TransactionId, tr);
                }

                VerifySignature(tr);
                await ValidateRefBlock(tr);

                trs.Add(tr);
            }

            return trs;
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

        private void VerifySignature(TransactionReceipt tr)
        {
            if (tr.SignatureSt != TransactionReceipt.Types.SignatureStatus.UnknownSignatureStatus)
            {
                return;
            }

            if(tr.Transaction.Sigs.Count > 1)
            {
                // check msig account authorization
                var validAuthorization = _authorizationInfo.CheckAuthority(tr.Transaction);
                if (!validAuthorization)
                {
                    tr.SignatureSt = TransactionReceipt.Types.SignatureStatus.SignatureInvalid;
                    return;
                }
            }
            
            var validSig = _signatureVerifier.Verify(tr.Transaction);
            tr.SignatureSt = validSig
                ? TransactionReceipt.Types.SignatureStatus.SignatureValid
                : TransactionReceipt.Types.SignatureStatus.SignatureInvalid;
        }

        private async Task ValidateRefBlock(TransactionReceipt tr)
        {
            if (tr.RefBlockSt != TransactionReceipt.Types.RefBlockStatus.UnknownRefBlockStatus &&
                tr.RefBlockSt != TransactionReceipt.Types.RefBlockStatus.FutureRefBlock)
            {
                return;
            }

            try
            {
                await _refBlockValidator.ValidateAsync(tr.Transaction);
                tr.RefBlockSt = TransactionReceipt.Types.RefBlockStatus.RefBlockValid;
            }
            catch (FutureRefBlockException)
            {
                tr.RefBlockSt = TransactionReceipt.Types.RefBlockStatus.FutureRefBlock;
            }
            catch (RefBlockInvalidException)
            {
                tr.RefBlockSt = TransactionReceipt.Types.RefBlockStatus.RefBlockInvalid;
            }
            catch (RefBlockExpiredException)
            {
                tr.RefBlockSt = TransactionReceipt.Types.RefBlockStatus.RefBlockExpired;
            }
        }

        private void IdentifyTransactionType(TransactionReceipt tr)
        {
            if (SystemAddresses.Contains(tr.Transaction.To))
            {
                tr.IsSystemTxn = true;
            }

            // cross chain txn should not be  broadcasted
            if (tr.Transaction.Type == TransactionType.CrossChainBlockInfoTransaction 
                && _crossChainContractAddress.Equals(tr.Transaction.To))
                tr.ToBeBroadCasted = false;
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
                    tr.Status = TransactionReceipt.Types.TransactionStatus.TransactionExecuted;
                    tr.ExecutedBlockNumber = blockNumber;
                    _transactionManager.AddTransactionAsync(tr.Transaction);
                    receipts.Add(tr);
                }
                else
                {
                    //TODO: Handle this, but it should never happen  
                }
            }

            _receiptManager.AddOrUpdateReceiptsAsync(receipts);
        }

        private void IdentifyExpiredTransactions()
        {
            if (_curHeight > GlobalConfig.ReferenceBlockValidPeriod)
            {
                var expired = _allTxns.Where(tr =>
                    tr.Value.Status == TransactionReceipt.Types.TransactionStatus.UnknownTransactionStatus
                    && tr.Value.RefBlockSt != TransactionReceipt.Types.RefBlockStatus.RefBlockExpired
                    && _curHeight > tr.Value.Transaction.RefBlockNumber
                    && _curHeight - tr.Value.Transaction.RefBlockNumber > GlobalConfig.ReferenceBlockValidPeriod
                );
                foreach (var tr in expired)
                {
                    tr.Value.RefBlockSt = TransactionReceipt.Types.RefBlockStatus.RefBlockExpired;
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

        private async Task RevalidateFutureTransactions()
        {
            // Re-validate FutureRefBlock transactions
            foreach (var tr in _allTxns.Values.Where(x =>
                x.RefBlockSt == TransactionReceipt.Types.RefBlockStatus.FutureRefBlock))
            {
                await ValidateRefBlock(tr);
            }
        }

        // Render transactions to expire, and purge old transactions (RefBlockValidPeriod + some buffer)
        public async Task OnNewBlock(Block block)
        {
            var blockHeader = block.Header;
            // TODO: Handle LIB
            if (blockHeader.Index > (_curHeight + 1) && _curHeight != GlobalConfig.GenesisBlockHeight)
            {
                throw new Exception($"Invalid block index {blockHeader.Index} but current height is {_curHeight}.");
            }

            _curHeight = blockHeader.Index;

            UpdateExecutedTransactions(block.Body.Transactions, block.Header.Index);

            IdentifyExpiredTransactions();

            RemoveOldTransactions();

            await RevalidateFutureTransactions();
        }

        private async Task OnBranchRolledBack(List<Block> blocks)
        {
            var minBN = blocks.Select(x => x.Header.Index).Min();

            // Valid and Invalid RefBlock becomes unknown
            foreach (var tr in _allTxns.Where(x => x.Value.Transaction.RefBlockNumber >= minBN))
            {
                tr.Value.RefBlockSt = TransactionReceipt.Types.RefBlockStatus.FutureRefBlock;
            }

            // Executed transactions added back to pending
            foreach (var b in blocks.OrderByDescending(b => b.Header.Index))
            {
                foreach (var txId in b.Body.Transactions)
                {
                    if (!_allTxns.TryGetValue(txId, out var tr))
                    {
                        var t = await _transactionManager.GetTransaction(txId);
                        tr = new TransactionReceipt(t);
                    }
                    
                    // cross chain type and dpos type transaction should not be reverted.
                    if (tr.Transaction.Type == TransactionType.CrossChainBlockInfoTransaction
                        && tr.Transaction.To.Equals(_crossChainContractAddress) ||
                        tr.Transaction.Type == TransactionType.DposTransaction
                        && tr.Transaction.To.Equals(_dPosContractAddress) && tr.Transaction.ShouldNotBroadcast())
                        continue;
                    
                    tr.SignatureSt = TransactionReceipt.Types.SignatureStatus.SignatureValid;
                    tr.Status = TransactionReceipt.Types.TransactionStatus.UnknownTransactionStatus;
                    tr.ExecutedBlockNumber = 0;
                    if (tr.Transaction.RefBlockNumber >= minBN)
                    {
                        tr.RefBlockSt = TransactionReceipt.Types.RefBlockStatus.FutureRefBlock;
                    }
                }
            }
        }

        #endregion
    }
}