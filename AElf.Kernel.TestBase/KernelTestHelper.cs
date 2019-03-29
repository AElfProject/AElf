using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel
{
    public class KernelTestHelper
    {
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
        ///             Height: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9 -> 10 -> 11 -> 12 -> 13 -> 14
        ///        Best Branch: a -> b -> c -> d -> e -> f -> g -> h -> i -> j  -> k
        ///     Longest Branch:                                   (h)-> l -> m  -> n  -> o  -> p 
        ///        Fork Branch:                    (e)-> q -> r -> s -> t -> u
        ///    Unlinked Branch:                                              v  -> w  -> x  -> y  -> z
        /// </returns>
        public async Task<Chain> MockChain()
        {
            var chain = await CreateChain();

            var genesisBlock = await _blockchainService.GetBlockByHashAsync(chain.GenesisBlockHash);
            BestBranchBlockList.Add(genesisBlock);
            
            BestBranchBlockList.AddRange(await AddBestBranch(chain));
            
            LongestBranchBlockList =
                await AddForkBranch(chain, BestBranchBlockList[7].Height, BestBranchBlockList[7].GetHash());

            foreach (var block in LongestBranchBlockList)
            {
                var chainBlockLink = await _chainManager.GetChainBlockLinkAsync(block.GetHash());
                await _chainManager.SetChainBlockLinkExecutionStatus(chainBlockLink,
                    ChainBlockLinkExecutionStatus.ExecutionFailed);
            }
            
            ForkBranchBlockList =
                await AddForkBranch(chain, BestBranchBlockList[4].Height, BestBranchBlockList[4].GetHash());

            UnlinkedBranchBlockList =
                await AddForkBranch(chain, 9, Hash.FromString("UnlinkBlock"));
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
                From = Address.Zero,
                To = Address.Zero,
                MethodName = Guid.NewGuid().ToString(),
                RefBlockNumber = refBlockNumber,
                RefBlockPrefix = refBlockHash == null
                    ? ByteString.Empty
                    : ByteString.CopyFrom(refBlockHash.DumpByteArray().Take(4).ToArray())
            };

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

        public Block GenerateBlock(long previousBlockHeight, Hash previousBlockHash, List<Transaction> transactions)
        {
            var newBlock = new Block
            {
                Header = new BlockHeader
                {
                    Height = previousBlockHeight + 1,
                    PreviousBlockHash = previousBlockHash,
                    Time = Timestamp.FromDateTime(DateTime.UtcNow)
                },
                Body = new BlockBody()
            };
            foreach (var transaction in transactions)
            {
                newBlock.AddTransaction(transaction);
            }

            newBlock.Header.MerkleTreeRootOfTransactions = newBlock.Body.CalculateMerkleTreeRoots();

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

                await _transactionResultService.AddTransactionResultAsync(transactionResult, newBlock.Header);
            }
            newBlock.Header.Bloom = ByteString.CopyFrom(bloom.Data);

            await _blockchainService.AddBlockAsync(newBlock);
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
            await _chainManager.SetChainBlockLinkExecutionStatus(chainBlockLink,
                ChainBlockLinkExecutionStatus.ExecutionSuccess);

            return block;
        }

        #region private methods

        private async Task<Chain> CreateChain()
        {
            var genesisBlock = new Block
            {
                Header = new BlockHeader
                {
                    Height = KernelConstants.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Empty
                },
                Body = new BlockBody()
            };
            var chain = await _blockchainService.CreateChainAsync(genesisBlock);
            return chain;
        }
        
        private async Task<List<Block>> AddBestBranch(Chain chain)
        {
            var bestBranchBlockList = new List<Block>();

            for (var i = 0; i < 10; i++)
            {
                chain = await _blockchainService.GetChainAsync();
                var newBlock = await AttachBlock(chain.BestChainHeight, chain.BestChainHash);
                bestBranchBlockList.Add(newBlock);
                
                var chainBlockLink = await _chainManager.GetChainBlockLinkAsync(newBlock.GetHash());
                await _chainManager.SetChainBlockLinkExecutionStatus(chainBlockLink,
                    ChainBlockLinkExecutionStatus.ExecutionSuccess);
                
                chain = await _blockchainService.GetChainAsync();
                await _blockchainService.SetBestChainAsync(chain, newBlock.Height, newBlock.GetHash());
            }

            return bestBranchBlockList;
        }
        
        private async Task<List<Block>> AddForkBranch(Chain chain, long previousHeight, Hash previousHash)
        {
            var forkBranchBlockList = new List<Block>();

            for (var i = 0; i < 5; i++)
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