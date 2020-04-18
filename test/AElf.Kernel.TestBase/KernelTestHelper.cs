using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel
{
    public class KernelTestHelper
    {
        public ECKeyPair KeyPair = CryptoHelper.GenerateKeyPair();
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionResultService _transactionResultService;
        private readonly IChainManager _chainManager;

        /// <summary>
        /// 12 Blocks: a -> b -> c -> d -> e -> f -> g -> h -> i -> j -> k
        /// </summary>
        public List<Block> BestBranchBlockList { get; set; }

        /// <summary>
        /// 5 Blocks: l -> m -> n -> o -> p 
        /// </summary>
        public List<Block> LongestBranchBlockList { get; set; }

        /// <summary>
        /// 5 Blocks: q -> r -> s -> t -> u
        /// </summary>
        public List<Block> ForkBranchBlockList { get; set; }

        /// <summary>
        /// 5 Blocks: v -> w -> x -> y -> z
        /// </summary>
        public List<Block> UnlinkedBranchBlockList { get; set; }

        public KernelTestHelper(IBlockchainService blockchainService,
            ITransactionResultService transactionResultService,
            IChainManager chainManager)
        {
            BestBranchBlockList = new List<Block>();
            LongestBranchBlockList = new List<Block>();
            ForkBranchBlockList = new List<Block>();
            UnlinkedBranchBlockList = new List<Block>();

            _blockchainService = blockchainService;
            _transactionResultService = transactionResultService;
            _chainManager = chainManager;
        }

        /// <summary>
        /// Mock a chain with a best branch, and some fork branches
        /// </summary>
        /// <returns>
        ///       Mock Chain
        ///    BestChainHeight: 11
        /// LongestChainHeight: 13
        ///         LIB height: 5
        /// 
        ///             Height: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9 -> 10 -> 11 -> 12 -> 13 -> 14 -> 15 -> 16 -> 17 -> 18 -> 19
        ///        Best Branch: a -> b -> c -> d -> e -> f -> g -> h -> i -> j  -> k
        ///     Longest Branch:                                   (h)-> l -> m  -> n  -> o  ->  p ->  q ->  r ->  s ->  t ->  u ->  v
        ///        Fork Branch:                    (e)-> q -> r -> s -> t -> u
        ///    Unlinked Branch:                                              v  -> w  -> x  -> y  -> z
        /// </returns>
        public async Task<Chain> MockChainAsync()
        {
            var chain = await CreateChain();

            var genesisBlock = await _blockchainService.GetBlockByHashAsync(chain.GenesisBlockHash);
            BestBranchBlockList.Add(genesisBlock);

            BestBranchBlockList.AddRange(await AddBestBranch());

            LongestBranchBlockList =
                await AddForkBranch(BestBranchBlockList[7].Height, BestBranchBlockList[7].GetHash(), 11);

            foreach (var block in LongestBranchBlockList)
            {
                var chainBlockLink = await _chainManager.GetChainBlockLinkAsync(block.GetHash());
                await _chainManager.SetChainBlockLinkExecutionStatusAsync(chainBlockLink,
                    ChainBlockLinkExecutionStatus.ExecutionFailed);
            }

            ForkBranchBlockList =
                await AddForkBranch(BestBranchBlockList[4].Height, BestBranchBlockList[4].GetHash());

            UnlinkedBranchBlockList = await AddForkBranch(9, HashHelper.ComputeFromString("UnlinkBlock"));
            // Set lib
            chain = await _blockchainService.GetChainAsync();
            await _blockchainService.SetIrreversibleBlockAsync(chain, BestBranchBlockList[4].Height,
                BestBranchBlockList[4].GetHash());

            return chain;
        }

        public Transaction GenerateTransaction(long refBlockNumber = 0, Hash refBlockHash = null)
        {
            var transaction = new Transaction
            {
                From = Address.FromPublicKey(KeyPair.PublicKey),
                To = SampleAddress.AddressList[0],
                MethodName = Guid.NewGuid().ToString(),
                Params = ByteString.Empty,
                RefBlockNumber = refBlockNumber,
                RefBlockPrefix = refBlockHash == null
                    ? ByteString.Empty
                    : ByteString.CopyFrom(refBlockHash.ToByteArray().Take(4).ToArray())
            };

            var signature = CryptoHelper.SignWithPrivateKey(KeyPair.PrivateKey, transaction.GetHash().ToByteArray());
            transaction.Signature = ByteString.CopyFrom(signature);
            return transaction;
        }

        public TransactionResult GenerateTransactionResult(Transaction transaction, TransactionResultStatus status,
            LogEvent logEvent = null)
        {
            var transactionResult = new TransactionResult
            {
                TransactionId = transaction.GetHash(),
                Status = status
            };

            if (logEvent != null)
            {
                transactionResult.Logs.Add(logEvent);
            }

            transactionResult.UpdateBloom();
            return transactionResult;
        }

        public Block GenerateBlock(long previousBlockHeight, Hash previousBlockHash,
            List<Transaction> transactions = null, Dictionary<string,ByteString> extraData = null)
        {

            var newBlock = new Block
            {
                Header = new BlockHeader
                {
                    Height = previousBlockHeight + 1,
                    PreviousBlockHash = previousBlockHash,
                    Time = TimestampHelper.GetUtcNow(),
                    MerkleTreeRootOfWorldState = Hash.Empty,
                    MerkleTreeRootOfTransactionStatus = Hash.Empty,
                    MerkleTreeRootOfTransactions = Hash.Empty,
                    SignerPubkey = ByteString.CopyFrom(KeyPair.PublicKey)
                },
                Body = new BlockBody()
            };

            if (extraData != null)
                newBlock.Header.ExtraData.Add(extraData);

            if (transactions != null)
            {
                foreach (var transaction in transactions)
                {
                    newBlock.AddTransaction(transaction);
                }

                newBlock.Header.MerkleTreeRootOfTransactions = newBlock.Body.CalculateMerkleTreeRoot();
            }

            return newBlock;
        }

        public async Task<Block> AttachBlock(long previousBlockHeight, Hash previousBlockHash,
            List<Transaction> transactions = null, List<TransactionResult> transactionResults = null)
        {
            if (transactions == null || transactions.Count == 0)
            {
                transactions = new List<Transaction>();
            }

            if (transactions.Count == 0)
            {
                transactions.Add(GenerateTransaction());
            }

            if (transactionResults == null)
            {
                transactionResults = new List<TransactionResult>();
            }

            if (transactionResults.Count == 0)
            {
                foreach (var transaction in transactions)
                {
                    transactionResults.Add(GenerateTransactionResult(transaction, TransactionResultStatus.Mined));
                }
            }

            var newBlock = GenerateBlock(previousBlockHeight, previousBlockHash, transactions);

            var bloom = new Bloom();
            foreach (var transactionResult in transactionResults)
            {
                transactionResult.UpdateBloom();
                if (transactionResult.Status == TransactionResultStatus.Mined)
                {
                    bloom.Combine(new[] {new Bloom(transactionResult.Bloom.ToByteArray())});
                }
            }

            newBlock.Header.Bloom = ByteString.CopyFrom(bloom.Data);

            await _transactionResultService.AddTransactionResultsAsync(transactionResults, newBlock.Header);

            await _blockchainService.AddBlockAsync(newBlock);
            await _blockchainService.AddTransactionsAsync(transactions);
            var chain = await _blockchainService.GetChainAsync();
            await _blockchainService.AttachBlockToChainAsync(chain, newBlock);

            return newBlock;
        }

        public async Task<Block> AttachBlockToBestChain(List<Transaction> transactions = null,
            List<TransactionResult> transactionResults = null)
        {
            var chain = await _blockchainService.GetChainAsync();
            var block = await AttachBlock(chain.BestChainHeight, chain.BestChainHash, transactions, transactionResults);

            chain = await _blockchainService.GetChainAsync();
            await _blockchainService.SetBestChainAsync(chain, block.Height, block.GetHash());

            var chainBlockLink = await _chainManager.GetChainBlockLinkAsync(block.GetHash());
            await _chainManager.SetChainBlockLinkExecutionStatusAsync(chainBlockLink,
                ChainBlockLinkExecutionStatus.ExecutionSuccess);

            return block;
        }

        public async Task<ChainContext> GetChainContextAsync()
        {
            var chain = await _blockchainService.GetChainAsync();
            return new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
        }

        #region private methods

        private async Task<Chain> CreateChain()
        {
            var genesisBlock = GenerateBlock(0, Hash.Empty, new List<Transaction>());

            var chain = await _blockchainService.CreateChainAsync(genesisBlock, new List<Transaction>());

            return chain;
        }

        private async Task<List<Block>> AddBestBranch()
        {
            var bestBranchBlockList = new List<Block>();

            for (var i = 0; i < 10; i++)
            {
                var chain = await _blockchainService.GetChainAsync();
                var newBlock = await AttachBlock(chain.BestChainHeight, chain.BestChainHash);
                bestBranchBlockList.Add(newBlock);

                var chainBlockLink = await _chainManager.GetChainBlockLinkAsync(newBlock.GetHash());
                await _chainManager.SetChainBlockLinkExecutionStatusAsync(chainBlockLink,
                    ChainBlockLinkExecutionStatus.ExecutionSuccess);

                chain = await _blockchainService.GetChainAsync();
                await _blockchainService.SetBestChainAsync(chain, newBlock.Height, newBlock.GetHash());
            }

            return bestBranchBlockList;
        }

        private async Task<List<Block>> AddForkBranch(long previousHeight, Hash previousHash, int count = 5)
        {
            var forkBranchBlockList = new List<Block>();

            for (var i = 0; i < count; i++)
            {
                var newBlock = await AttachBlock(previousHeight, previousHash);
                forkBranchBlockList.Add(newBlock);

                previousHeight++;
                previousHash = newBlock.GetHash();
            }

            return forkBranchBlockList;
        }

        #endregion
    }
}