using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.Dividend;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.Token;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Kernel.Types.SmartContract;
using AElf.OS.Node.Application;
using AElf.OS.Node.Domain;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Volo.Abp.Threading;

namespace AElf.OS
{
    public class OSTestHelper
    {
        private readonly ChainOptions _chainOptions;
        
        private readonly IOsBlockchainNodeContextService _osBlockchainNodeContextService;
        private readonly IAccountService _accountService;
        private readonly IMinerService _minerService;
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITxHub _txHub;
        private readonly IStaticChainInformationProvider _staticChainInformationProvider;
        private readonly IBlockAttachService _blockAttachService;
        
        private OsBlockchainNodeContext _blockchainNodeCtxt;
        
        /// <summary>
        /// 12 Blocks: a -> b -> c -> d -> e -> f -> g -> h -> i -> j -> k
        /// </summary>
        public List<Block> BestBranchBlockList { get; set; }
        
        /// <summary>
        /// 5 Blocks: q -> r -> s -> t -> u
        /// </summary>
        public List<Block> ForkBranchBlockList { get; set; }
        
        /// <summary>
        /// 5 Blocks: v -> w -> x -> y -> z
        /// </summary>
        public List<Block> UnlinkedBranchBlockList { get; set; }

        public OSTestHelper(IOsBlockchainNodeContextService osBlockchainNodeContextService,
            IAccountService accountService,
            IMinerService minerService,
            IBlockchainService blockchainService,
            ITxHub txHub,
            ISmartContractAddressService smartContractAddressService,
            IBlockAttachService blockAttachService,
            IStaticChainInformationProvider staticChainInformationProvider,
            IOptionsSnapshot<ChainOptions> chainOptions)
        {
            _chainOptions = chainOptions.Value;

            _osBlockchainNodeContextService = osBlockchainNodeContextService;
            _accountService = accountService;
            _minerService = minerService;
            _blockchainService = blockchainService;
            _smartContractAddressService = smartContractAddressService;
            _blockAttachService = blockAttachService;
            _txHub = txHub;
            _staticChainInformationProvider = staticChainInformationProvider;

            BestBranchBlockList = new List<Block>();
            ForkBranchBlockList = new List<Block>();
            UnlinkedBranchBlockList = new List<Block>();
        }

        /// <summary>
        /// Mock a chain with a best branch, and some fork branches
        /// </summary>
        /// <returns>
        ///       Mock Chain
        ///    BestChainHeight: 11
        ///         LIB height: 5
        /// 
        ///             Height: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9 -> 10 -> 11 -> 12 -> 13 -> 14
        ///        Best Branch: a -> b -> c -> d -> e -> f -> g -> h -> i -> j  -> k
        ///        Fork Branch:                    (e)-> q -> r -> s -> t -> u
        ///    Unlinked Branch:                                              v  -> w  -> x  -> y  -> z
        /// </returns>
        public async Task MockChain()
        {
            await StartNode();
            var chain = await _blockchainService.GetChainAsync();

            var genesisBlock = await _blockchainService.GetBlockByHashAsync(chain.GenesisBlockHash);
            BestBranchBlockList.Add(genesisBlock);

            BestBranchBlockList.AddRange(await AddBestBranch());

            ForkBranchBlockList =
                await AddForkBranch(BestBranchBlockList[4].GetHash(), BestBranchBlockList[4].Height);

            UnlinkedBranchBlockList = await AddForkBranch(Hash.FromString("UnlinkBlock"), 9);

            // Set lib
            chain = await _blockchainService.GetChainAsync();
            await _blockchainService.SetIrreversibleBlockAsync(chain, BestBranchBlockList[4].Height,
                BestBranchBlockList[4].GetHash());
        }

        public async Task DisposeMock()
        {
            await StopNode();
        }

        public async Task<Transaction> GenerateTransferTransaction()
        {
            var newUserKeyPair = CryptoHelpers.GenerateKeyPair();
            var accountAddress = await _accountService.GetAccountAsync();

            var transaction = GenerateTransaction(accountAddress,
                _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name),
                nameof(TokenContract.Transfer),
                new TransferInput {To = Address.FromPublicKey(newUserKeyPair.PublicKey), Amount = 10, Symbol = "ELF"});

            var signature = await _accountService.SignAsync(transaction.GetHash().DumpByteArray());
            transaction.Sigs.Add(ByteString.CopyFrom(signature));

            return transaction;
        }

        public async Task<List<Transaction>> GenerateTransferTransactions(int count)
        {
            var transactions = new List<Transaction>();
            for (var i = 0; i < count; i++)
            {
                var transaction = await GenerateTransferTransaction();
                transactions.Add(transaction);
            }

            return transactions;
        }

        public Transaction GenerateTransaction(Address from, Address to,string methodName, IMessage input)
        {
            var chain = _blockchainService.GetChainAsync().Result;
            var transaction = new Transaction
            {
                From = from,
                To = to,
                MethodName = methodName,
                Params = input.ToByteString(),
                RefBlockNumber = chain.BestChainHeight,
                RefBlockPrefix = ByteString.CopyFrom(chain.BestChainHash.Value.Take(4).ToArray()),
            };

            return transaction;
        }

        public async Task BroadcastTransactions(List<Transaction> transactions)
        {
            var transactionsReceivedEvent = new TransactionsReceivedEvent
            {
                Transactions = transactions
            };

            await _txHub.HandleTransactionsReceivedAsync(transactionsReceivedEvent);
        }

        public async Task<Block> MinedOneBlock(Hash previousBlockHash = null, long previousBlockHeight = 0)
        {
            if (previousBlockHash == null || previousBlockHeight == 0)
            {
                var chain = await _blockchainService.GetChainAsync();
                previousBlockHash = chain.BestChainHash;
                previousBlockHeight = chain.BestChainHeight;
            }

            var block = await _minerService.MineAsync(previousBlockHash, previousBlockHeight,
                DateTime.UtcNow, TimeSpan.FromMilliseconds(4000));

            await _blockAttachService.AttachBlockAsync(block);
                
            return block;
        }

        public Block GenerateBlock(Hash preBlockHash, long preBlockHeight, List<Transaction> transactions)
        {
            var block = new Block
            {
                Header = new BlockHeader
                {
                    ChainId = _staticChainInformationProvider.ChainId,
                    Height = preBlockHeight + 1,
                    PreviousBlockHash = preBlockHash,
                    Time = Timestamp.FromDateTime(DateTime.UtcNow)
                },
                Body = new BlockBody()
            };
            foreach (var transaction in transactions)
            {
                block.AddTransaction(transaction);
            }

            block.Header.MerkleTreeRootOfTransactions = block.Body.CalculateMerkleTreeRoots();

            return block;
        }


        #region private methods

        private async Task StartNode()
        {
            var dto = new OsBlockchainNodeContextStartDto
            {
                ZeroSmartContract = typeof(BasicContractZero),
                ChainId = _chainOptions.ChainId
            };

            dto.InitializationSmartContracts.AddConsensusSmartContract<ConsensusContract>();

            var ownAddress = await _accountService.GetAccountAsync();
            var callList = new SystemTransactionMethodCallList();
            callList.Add(nameof(TokenContract.CreateNativeToken), new CreateInput
            {
                Symbol = "ELF",
                TokenName = "ELF_Token",
                TotalSupply = 1000_0000L,
                Decimals = 2,
                Issuer =  ownAddress,
                IsBurnable = true
            });
            callList.Add(nameof(TokenContract.Issue), new IssueInput
            {
                Symbol = "ELF",
                Amount = 1000_0000L,
                To = ownAddress,
                Memo = "Issue"
            });
            
            dto.InitializationSmartContracts.AddGenesisSmartContract<DividendContract>(
                DividendsSmartContractAddressNameProvider.Name);
            dto.InitializationSmartContracts.AddGenesisSmartContract<TokenContract>(
                TokenSmartContractAddressNameProvider.Name, callList);

            _blockchainNodeCtxt = await _osBlockchainNodeContextService.StartAsync(dto);
        }

        private async Task StopNode()
        {
            await _osBlockchainNodeContextService.StopAsync(_blockchainNodeCtxt);
        }

        private async Task<List<Block>> AddBestBranch()
        {
            var bestBranchBlockList = new List<Block>();

            for (var i = 0; i < 10; i++)
            {
                var chain = await _blockchainService.GetChainAsync();
                var transaction = await GenerateTransferTransaction();
                await BroadcastTransactions(new List<Transaction> {transaction});
                var block = await MinedOneBlock(chain.BestChainHash, chain.BestChainHeight);
                
                bestBranchBlockList.Add(block);
            }

            return bestBranchBlockList;
        }
        
        private async Task<List<Block>> AddForkBranch(Hash previousHash, long previousHeight)
        {
            var forkBranchBlockList = new List<Block>();

            for (var i = 0; i < 5; i++)
            {
//                var transaction = await GenerateTransferTransaction();
//                await BroadcastTransactions(new List<Transaction> {transaction});
                var block = await MinedOneBlock(previousHash,previousHeight);
                
                forkBranchBlockList.Add(block);

                previousHeight++;
                previousHash = block.GetHash();
            }

            return forkBranchBlockList;
        }
        
        #endregion
    }
}