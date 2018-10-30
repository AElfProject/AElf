using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common;
using AElf.Configuration;
using AElf.Kernel;
using AElf.Kernel.EventMessages;
using AElf.Kernel.Managers;
using AElf.Miner.EventMessages;
using AElf.Miner.TxMemPool.RefBlockExceptions;
using Easy.MessageHub;

namespace AElf.Miner.TxMemPool
{
    public class TxHub : ITxHub
    {
        private readonly ITransactionManager _transactionManager;
        private readonly ITransactionReceiptManager _receiptManager;
        private readonly ITxSignatureVerifier _signatureVerifier;
        private readonly ITxRefBlockValidator _refBlockValidator;

        private readonly ConcurrentDictionary<Hash, TransactionReceipt> _allTxns =
            new ConcurrentDictionary<Hash, TransactionReceipt>();

        private readonly IChainService _chainService;
        private IBlockChain _blockChain;

        private IBlockChain BlockChain => _blockChain ??
                                          (_blockChain =
                                              _chainService.GetBlockChain(Hash.LoadHex(NodeConfig.Instance.ChainId)));

        private ulong _curHeight;

        private ulong CurHeight
        {
            get
            {
                if (_curHeight == 0)
                {
                    _curHeight = BlockChain.GetCurrentBlockHeightAsync().Result;
                }

                return _curHeight;
            }
        }

        public TxHub(ITransactionManager transactionManager, ITransactionReceiptManager receiptManager,
            IChainService chainService,
            ITxSignatureVerifier signatureVerifier,
            ITxRefBlockValidator refBlockValidator)
        {
            _transactionManager = transactionManager;
            _receiptManager = receiptManager;
            _chainService = chainService;
            _signatureVerifier = signatureVerifier;
            _refBlockValidator = refBlockValidator;

        }

        public void Initialize()
        {
            MessageHub.Instance.Subscribe<TransactionsExecuted>(OnTransactionsExecuted);
            MessageHub.Instance.Subscribe<BlockHeader>(OnNewBlockHeader);
            MessageHub.Instance.Subscribe<BranchRolledBack>(async branch =>
                await OnBranchRolledBack(branch.Blocks).ConfigureAwait(false));            
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
                throw new Exception("Transaction already exists.");
            }

            if (!_allTxns.TryAdd(tr.TransactionId, tr))
            {
                // Add failed, transaction exists already
                throw new Exception("Transaction already exists.");
            }

            IdentifyTransactionType(tr);

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
            if (tr.IsExecutable)
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

            var validSig = _signatureVerifier.Verify(tr.Transaction);
            tr.SignatureSt = validSig
                ? TransactionReceipt.Types.SignatureStatus.SignatureValid
                : TransactionReceipt.Types.SignatureStatus.SignatureInvalid;
        }

        private async Task ValidateRefBlock(TransactionReceipt tr)
        {
            if (tr.RefBlockSt != TransactionReceipt.Types.RefBlockStatus.UnknownRefBlockStatus)
            {
                return;
            }

            try
            {
                await _refBlockValidator.ValidateAsync(tr.Transaction);
                tr.RefBlockSt = TransactionReceipt.Types.RefBlockStatus.RefBlockValid;
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
            var systemAddresses = new List<Address>()
            {
                AddressHelpers.GetSystemContractAddress(
                    Hash.LoadHex(NodeConfig.Instance.ChainId),
                    SmartContractType.AElfDPoS.ToString()),
                AddressHelpers.GetSystemContractAddress(
                    Hash.LoadHex(NodeConfig.Instance.ChainId),
                    SmartContractType.SideChainContract.ToString())
            };
            if (systemAddresses.Contains(tr.Transaction.To))
            {
                tr.IsSystemTxn = true;
            }
        }

        #endregion Private Methods

        #region Event Handlers

        // Change transaction status and add transaction into TransactionManager.
        private void OnTransactionsExecuted(TransactionsExecuted transactionsesExecuted)
        {
            var receipts = new List<TransactionReceipt>();
            foreach (var tx in transactionsesExecuted.Transactions)
            {
                if (_allTxns.TryGetValue(tx.GetHash(), out var tr))
                {
                    tr.Status = TransactionReceipt.Types.TransactionStatus.TransactionExecuted;
                    tr.ExecutedBlockNumber = transactionsesExecuted.BlockNumber;
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

        // Render transactions to expire, and purge old transactions (RefBlockValidPeriod + some buffer)
        private void OnNewBlockHeader(BlockHeader blockHeader)
        {
            // TODO: Handle LIB
            if (blockHeader.Index != (CurHeight + 1) && CurHeight != 0)
            {
                throw new Exception("Invalid block index.");
            }

            _curHeight = blockHeader.Index;

            // Identify expired transactions
            if (CurHeight > GlobalConfig.ReferenceBlockValidPeriod)
            {
                var expired = _allTxns.Where(tr =>
                    tr.Value.Status == TransactionReceipt.Types.TransactionStatus.UnknownTransactionStatus
                    && tr.Value.RefBlockSt != TransactionReceipt.Types.RefBlockStatus.RefBlockExpired
                    && CurHeight - tr.Value.Transaction.RefBlockNumber > GlobalConfig.ReferenceBlockValidPeriod
                );
                foreach (var tr in expired)
                {
                    tr.Value.RefBlockSt = TransactionReceipt.Types.RefBlockStatus.RefBlockExpired;
                }
            }

            // TODO: Improve
            // Remove old transactions (executed, invalid and expired)
            var keepNBlocks = GlobalConfig.ReferenceBlockValidPeriod / 4 * 5;
            if (CurHeight - GlobalConfig.GenesisBlockHeight > keepNBlocks)
            {
                var blockNumberThreshold = CurHeight - keepNBlocks;
                var toRemove = _allTxns.Where(tr => tr.Value.Transaction.RefBlockNumber < blockNumberThreshold);
                foreach (var tr in toRemove)
                {
                    _allTxns.TryRemove(tr.Key, out _);
                }
            }
        }

        private async Task OnBranchRolledBack(List<Block> blocks)
        {
            var minBN = blocks.Select(x => x.Header.Index).Min();

            // Invalid RefBlock becomes unknown
            foreach (var tr in _allTxns.Where(x =>
                x.Value.Transaction.RefBlockNumber >= minBN &&
                x.Value.RefBlockSt == TransactionReceipt.Types.RefBlockStatus.RefBlockInvalid))
            {
                tr.Value.RefBlockSt = TransactionReceipt.Types.RefBlockStatus.UnknownRefBlockStatus;
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

                    tr.SignatureSt = TransactionReceipt.Types.SignatureStatus.SignatureValid;
                    tr.Status = TransactionReceipt.Types.TransactionStatus.UnknownTransactionStatus;
                    tr.ExecutedBlockNumber = 0;
                    if (tr.Transaction.RefBlockNumber >= minBN)
                    {
                        tr.RefBlockSt = TransactionReceipt.Types.RefBlockStatus.UnknownRefBlockStatus;
                    }
                }
            }
        }

        #endregion
    }
}