using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.EventMessages;
using AElf.Kernel.Managers;
using AElf.Kernel.Types.Transaction;
using AElf.Miner.EventMessages;
using AElf.Miner.TxMemPool.RefBlockExceptions;
using AElf.SmartContract.Consensus;
using AElf.SmartContract.Proposal;
using Easy.MessageHub;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Miner.TxMemPool
{
    public class TxHub : ITxHub, ISingletonDependency 
    {
        public ILogger<TxHub> Logger {get;set;}
        
        private readonly ITransactionManager _transactionManager;
        private readonly ITransactionReceiptManager _receiptManager;
        private readonly ITxSignatureVerifier _signatureVerifier;
        private readonly ITxRefBlockValidator _refBlockValidator;
        private readonly IAuthorizationInfoReader _authorizationInfoReader;
        private readonly IChainService _chainService;
        private readonly IElectionInfo _electionInfo;
        
        private readonly ConcurrentDictionary<Hash, TransactionReceipt> _allTxns =
            new ConcurrentDictionary<Hash, TransactionReceipt>();
        
        private IBlockChain _blockChain;
        
        private ulong _curHeight;

        private readonly int _chainId;

        private readonly Address _dPosContractAddress;
        private readonly Address _crossChainContractAddress;
        
        private List<Address> SystemAddresses => new List<Address>
        {
            _dPosContractAddress, 
            _crossChainContractAddress
        };

        public TxHub(ITransactionManager transactionManager, ITransactionReceiptManager receiptManager,
            IChainService chainService, IAuthorizationInfoReader authorizationInfoReader, ITxSignatureVerifier signatureVerifier,
            ITxRefBlockValidator refBlockValidator, IElectionInfo electionInfo)
        {
            Logger = NullLogger<TxHub>.Instance;
            _electionInfo = electionInfo;
            _transactionManager = transactionManager;
            _receiptManager = receiptManager;
            _chainService = chainService;
            _signatureVerifier = signatureVerifier;
            _refBlockValidator = refBlockValidator;
            _authorizationInfoReader = authorizationInfoReader;

            _chainId = ChainConfig.Instance.ChainId.ConvertBase58ToChainId();

            _dPosContractAddress = ContractHelpers.GetConsensusContractAddress(_chainId);
            _crossChainContractAddress =   ContractHelpers.GetCrossChainContractAddress(_chainId);
        }

        public void Initialize()
        {
            _blockChain = _chainService.GetBlockChain(_chainId);

            if (_blockChain == null)
            {
                Logger.LogWarning($"Could not find the blockchain for {_chainId}.");
                return;
            }

            _curHeight = _blockChain.GetCurrentBlockHeightAsync().Result;
                
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
                // Logger.LogWarning($"Transaction {transaction.GetHash()} already exists.");
                return;
            }

            if (!_allTxns.TryAdd(tr.TransactionId, tr))
            {
                // Logger.LogWarning($"Transaction {transaction.GetHash()} already exists.");
                return;
            }

            IdentifyTransactionType(tr);

            // todo this should be up to the caller of this method to choose
            // todo weither or not this is done on another thread, currently 
            // todo this gives the caller no choice.

            var task = Task.Run(async () =>
            {
                await VerifySignature(tr);
                await ValidateRefBlock(tr);
                MaybePublishTransaction(tr);
            });
        }

        public async Task<List<TransactionReceipt>> GetReceiptsOfExecutablesAsync()
        {
            return await Task.FromResult(_allTxns.Values.Where(x => x.IsExecutable).ToList());
        }

        public async Task<TransactionReceipt> GetCheckedReceiptsAsync(Transaction txn)
        {
            if (!_allTxns.TryGetValue(txn.GetHash(), out var tr))
            {
                tr = new TransactionReceipt(txn);
                _allTxns.TryAdd(tr.TransactionId, tr);
            }
            await VerifySignature(tr);
            await ValidateRefBlock(tr);
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

        private async Task VerifySignature(TransactionReceipt tr)
        {
            if (tr.SignatureSt != TransactionReceipt.Types.SignatureStatus.UnknownSignatureStatus)
            {
                return;
            }

            if(tr.Transaction.Sigs.Count > 1)
            {
                if (tr.Transaction.From.Equals(Address.Genesis))
                {
                    // validate miners authorization
                    var authorizationResult = await ValidateMinersAuthorization(tr);
                    if (!authorizationResult)
                    {
                        tr.SignatureSt = TransactionReceipt.Types.SignatureStatus.SignatureInvalid;
                        return;
                    }
                }
                else
                {
                    // validate authorization for multi-sig address
                    var validAuthorization = await CheckAuthority(tr.Transaction);
                    if (!validAuthorization)
                    {
                        tr.SignatureSt = TransactionReceipt.Types.SignatureStatus.SignatureInvalid;
                        return;
                    }
                }
                tr.SignatureSt = TransactionReceipt.Types.SignatureStatus.SignatureValid;
                return;
            }
            
            var validSig = _signatureVerifier.Verify(tr.Transaction);
            tr.SignatureSt = validSig
                ? TransactionReceipt.Types.SignatureStatus.SignatureValid
                : TransactionReceipt.Types.SignatureStatus.SignatureInvalid;
        }

        /// <summary>
        /// Validate miners authorization if it is a multi-sig transaction from Genesis address.
        /// </summary>
        /// <param name="tr"></param>
        /// <returns></returns>
        private async Task<bool> ValidateMinersAuthorization(TransactionReceipt tr)
        {
            var minerPublicKeysInHex = await _electionInfo.GetCurrentMines();
            var auth = new Authorization
            {
                MultiSigAccount =Address.Genesis,
                ExecutionThreshold = (uint) minerPublicKeysInHex.Count * 2 / 3,
                ProposerThreshold = 0
            };
            auth.Reviewers.AddRange(minerPublicKeysInHex.Select(r => new Reviewer
            {
                PubKey = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(r)),
                Weight = 1 // BP weight
            }));
            var hash = tr.Transaction.GetHash().DumpByteArray();
            var publicKeys = new List<byte[]>();
            foreach (var sig in tr.Transaction.Sigs)
            {
                var canBeRecovered = CryptoHelpers.RecoverPublicKey(sig.ToByteArray(), hash, out var publicKey);
                if (!canBeRecovered)
                    return false;
                publicKeys.Add(publicKey);
            }
            return _authorizationInfoReader.ValidateAuthorization(auth, publicKeys);
        }
        
        
        /// <summary>
        /// Check authority of multi-sig address.
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        private async Task<bool> CheckAuthority(Transaction transaction)
        {
            var sigCount = transaction.Sigs.Count;
            if (sigCount == 0)
                return false;
            
            // Get tx hash
            var hash = transaction.GetHash().DumpByteArray();

            if (transaction.Sigs.Count == 1)
                return true;
            // Get pub keys
            var publicKeys = new List<byte[]>();
            foreach (var sig in transaction.Sigs)
            {
                var canBeRecovered = CryptoHelpers.RecoverPublicKey(sig.ToByteArray(), hash, out var publicKey);
                if (!canBeRecovered)
                    return false;
                publicKeys.Add(publicKey);
            }
            
            return await _authorizationInfoReader.CheckAuthority(transaction.From, publicKeys);
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

            if (tr.Transaction.IsClaimFeesTransaction())
            {
                tr.IsSystemTxn = true;
            }

            // cross chain txn should not be  broadcasted
            if (tr.Transaction.IsCrossChainIndexingTransaction())
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
                    
                    // todo: quick fix for null txn after rollback
                    if(tr.Transaction == null)
                        continue;
                    
                    // cross chain transactions should not be reverted.
                    if (tr.Transaction.IsCrossChainIndexingTransaction())
                        continue;

                    if (tr.Transaction.IsClaimFeesTransaction())
                    {
                        continue;
                    }
                    
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