using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common;
using AElf.Configuration;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.EventMessages;
using AElf.Kernel.Managers;
using AElf.Miner.EventMessages;
using Easy.MessageHub;
using Google.Protobuf;
using NLog;

namespace AElf.Miner.TxMemPool
{
    public class NewTxHub
    {
        private ITransactionManager _transactionManager;

        private ConcurrentDictionary<Hash, TransactionReceipt> _allTxns =
            new ConcurrentDictionary<Hash, TransactionReceipt>();

        private IChainService _chainService;
        private IBlockChain _blockChain;
        private CanonicalBlockHashCache _canonicalBlockHashCache;

        private IBlockChain BlockChain
        {
            get
            {
                if (_blockChain == null)
                {
                    _blockChain =
                        _chainService.GetBlockChain(Hash.LoadHex(NodeConfig.Instance.ChainId));
                }

                return _blockChain;
            }
        }

        private ulong _curHeight;

        public ulong CurHeight
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

        public Action<TransactionReceipt> SignatureValidator { get; set; } = (tr) =>
        {
            Task.Run(() =>
            {
                if (VerifySignature(tr.Transaction))
                {
                    tr.SignatureSt = TransactionReceipt.Types.SignatureStatus.SignatureValid;
                    MaybePublishTransaction(tr);
                }
                else
                {
                    tr.SignatureSt = TransactionReceipt.Types.SignatureStatus.SignatureInvalid;
                }
            }).ConfigureAwait(false);
        };

        public Action<NewTxHub, TransactionReceipt> RefBlockValidator { get; set; } = (hub, tr) =>
        {
            Task.Run(async () =>
            {
                tr.RefBlockSt = await hub.ValidateReferenceBlockAsync(tr.Transaction);
                MaybePublishTransaction(tr);
            }).ConfigureAwait(false);
        };

        public Action<TransactionReceipt> SystemTxnIdentifier { get; set; } = (tr) =>
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
        };

        public NewTxHub(ITransactionManager transactionManager, IChainService chainService)
        {
            _transactionManager = transactionManager;
            _chainService = chainService;
            _canonicalBlockHashCache = new CanonicalBlockHashCache(BlockChain, LogManager.GetLogger(nameof(NewTxHub)));
            MessageHub.Instance.Subscribe<TransactionsExecuted>(OnTransactionsExecuted);
            MessageHub.Instance.Subscribe<BlockHeader>(OnNewBlockHeader);
            MessageHub.Instance.Subscribe<BranchRolledBack>(async branch =>
                await OnBranchRolledBack(branch.Blocks).ConfigureAwait(false));
        }

        // This may be moved to extension method
        private static bool VerifySignature(Transaction tx)
        {
            if (tx.P == null)
            {
                return false;
            }

            byte[] uncompressedPrivKey = tx.P.ToByteArray();
            var addr = Address.FromRawBytes(uncompressedPrivKey);

            if (!addr.Equals(tx.From))
                return false;
            ECKeyPair recipientKeyPair = ECKeyPair.FromPublicKey(uncompressedPrivKey);
            ECVerifier verifier = new ECVerifier(recipientKeyPair);
            return verifier.Verify(tx.GetSignature(), tx.GetHash().DumpByteArray());
        }

        public async Task AddTransactionAsync(Transaction transaction)
        {
            var tr = new TransactionReceipt(transaction);

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

            if (SignatureValidator == null)
            {
                throw new Exception($"{nameof(SignatureValidator)} is not set.");
            }

            SystemTxnIdentifier.Invoke(tr);
            SignatureValidator.Invoke(tr);
            RefBlockValidator.Invoke(this, tr);
        }

        public Task<IEnumerable<TransactionReceipt>> GetReadyTxsAsync()
        {
            return Task.FromResult(_allTxns.Values.Where(x => x.IsExecutable));
        }

        public async Task<List<TransactionReceipt>> ValidateAllTxs(IEnumerable<Transaction> transactions)
        {
            var trs = new List<TransactionReceipt>();
            // TODO: Check if parallelization is needed
            foreach (var txn in transactions)
            {
                if (!_allTxns.TryGetValue(txn.GetHash(), out var tr))
                {
                    tr = new TransactionReceipt(txn);
                }

                // Verify Signature if it is not already done
                if (tr.SignatureSt == TransactionReceipt.Types.SignatureStatus.UnknownSignatureStatus)
                {
                    tr.SignatureSt = VerifySignature(tr.Transaction)
                        ? TransactionReceipt.Types.SignatureStatus.SignatureValid
                        : TransactionReceipt.Types.SignatureStatus.SignatureInvalid;
                }

                // Verify RefBlock if it is not already done
                if (tr.RefBlockSt == TransactionReceipt.Types.RefBlockStatus.UnknownRefBlockStatus)
                {
                    tr.RefBlockSt = await ValidateReferenceBlockAsync(tr.Transaction);
                }
                trs.Add(tr);
            }

            return trs;
        }

        public async Task<TransactionReceipt> GetTxReceiptAsync(Hash txId)
        {
            _allTxns.TryGetValue(txId, out var tr);
            return await Task.FromResult(tr);
        }

        public async Task<Transaction> GetTxAsync(Hash txId)
        {
            if (_allTxns.TryGetValue(txId, out var tr))
            {
                return await Task.FromResult(tr.Transaction);
            }

            return await _transactionManager.GetTransaction(txId);
        }

        private static bool CheckPrefix(Hash blockHash, ByteString prefix)
        {
            if (prefix.Length > blockHash.Value.Length)
            {
                return false;
            }

            return !prefix.Where((t, i) => t != blockHash.Value[i]).Any();
        }

        public async Task<TransactionReceipt.Types.RefBlockStatus> ValidateReferenceBlockAsync(Transaction tx)
        {
            if (tx.RefBlockNumber < GlobalConfig.GenesisBlockHeight && CheckPrefix(Hash.Genesis, tx.RefBlockPrefix))
            {
                return TransactionReceipt.Types.RefBlockStatus.RefBlockValid;
            }

            var curHeight = _canonicalBlockHashCache.CurrentHeight;
            if (tx.RefBlockNumber > curHeight && curHeight > GlobalConfig.GenesisBlockHeight)
            {
                return TransactionReceipt.Types.RefBlockStatus.RefBlockInvalid;
            }

            if (curHeight > GlobalConfig.ReferenceBlockValidPeriod + GlobalConfig.GenesisBlockHeight &&
                curHeight - tx.RefBlockNumber > GlobalConfig.ReferenceBlockValidPeriod)
            {
                return TransactionReceipt.Types.RefBlockStatus.RefBlockExpired;
            }

            Hash canonicalHash;
            if (curHeight == 0)
            {
                canonicalHash = await BlockChain.GetCurrentBlockHashAsync();
            }
            else
            {
                canonicalHash = _canonicalBlockHashCache.GetHashByHeight(tx.RefBlockNumber);
            }

            if (canonicalHash == null)
            {
                canonicalHash = (await BlockChain.GetBlockByHeightAsync(tx.RefBlockNumber)).GetHash();
            }

            if (canonicalHash == null)
            {
                throw new Exception(
                    $"Unable to get canonical hash for height {tx.RefBlockNumber} - current height: {curHeight}");
            }

            // TODO: figure out why do we need this
            if (GlobalConfig.BlockProducerNumber == 1)
            {
                return TransactionReceipt.Types.RefBlockStatus.RefBlockValid;
            }

            var res = CheckPrefix(canonicalHash, tx.RefBlockPrefix)
                ? TransactionReceipt.Types.RefBlockStatus.RefBlockValid
                : TransactionReceipt.Types.RefBlockStatus.RefBlockInvalid;
            return res;
        }

        private static void MaybePublishTransaction(TransactionReceipt tr)
        {
            if (tr.IsExecutable)
            {
                MessageHub.Instance.Publish(new TransactionAddedToPool(tr.Transaction));
            }
        }

        #region Event Handlers

        // Change transaction status and add transaction into TransactionManager.
        private void OnTransactionsExecuted(TransactionsExecuted transactionsesExecuted)
        {
            foreach (var tx in transactionsesExecuted.Transactions)
            {
                if (_allTxns.TryGetValue(tx.GetHash(), out var tr))
                {
                    tr.Status = TransactionReceipt.Types.TransactionStatus.TransactionExecuted;
                    tr.ExecutedBlockNumber = transactionsesExecuted.BlockNumber;
                    _transactionManager.AddTransactionAsync(tr.Transaction);
                }
            }
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