using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.Genesis;
using AElf.Contracts.Token;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Kernel.Types.SmartContract;
using AElf.OS.Node.Application;
using AElf.Types.CSharp;
using Google.Protobuf;
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
            IStaticChainInformationProvider staticChainInformationProvider,
            IOptionsSnapshot<ChainOptions> chainOptions)
        {
            _chainOptions = chainOptions.Value;

            _osBlockchainNodeContextService = osBlockchainNodeContextService;
            _accountService = accountService;
            _minerService = minerService;
            _blockchainService = blockchainService;
            _smartContractAddressService = smartContractAddressService;
            _staticChainInformationProvider = staticChainInformationProvider;
            _txHub = txHub;
            

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

        public async Task<Transaction> GenerateTransferTransaction()
        {
            var newUserKeyPair = CryptoHelpers.GenerateKeyPair();
            var accountAddress = await _accountService.GetAccountAsync();

            var transaction = GenerateTransaction(accountAddress,
                _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name),
                nameof(TokenContract.Transfer),
                Address.FromPublicKey(newUserKeyPair.PublicKey), 10);

            var signature = await _accountService.SignAsync(transaction.GetHash().DumpByteArray());
            transaction.Sigs.Add(ByteString.CopyFrom(signature));

            return transaction;
        }
        
        public Transaction GenerateTransaction(Address from, Address to,string methodName, params object[] objects)
        {
            var chain = _blockchainService.GetChainAsync().Result;
            var transaction = new Transaction
            {
                From = from,
                To = to,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(objects)),
                RefBlockNumber = chain.BestChainHeight,
                RefBlockPrefix = ByteString.CopyFrom(chain.BestChainHash.DumpByteArray().Take(4).ToArray())
            };

            return transaction;
        }

        public async Task BroadcastTransaction(List<Transaction> transactions)
        {
            var transactionsReceivedEvent = new TransactionsReceivedEvent
            {
                Transactions = transactions
            };

            await _txHub.HandleTransactionsReceivedAsync(transactionsReceivedEvent);
        }

        public async Task<Block> MinedOneBlock(Hash previousBlockHash, long previousBlockHeight)
        {
            var block = await _minerService.MineAsync(previousBlockHash, previousBlockHeight,
                DateTime.UtcNow.AddMilliseconds(4000));

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

            dto.InitializationSmartContracts.AddGenesisSmartContract<TokenContract>(
                TokenSmartContractAddressNameProvider.Name);

            var accountAddress = await _accountService.GetAccountAsync();
            var transactions = GetGenesisTransactions(accountAddress,
                _staticChainInformationProvider.GetSystemContractAddressInGenesisBlock(2));

            dto.InitializationTransactions = transactions;

            await _osBlockchainNodeContextService.StartAsync(dto);
        }

        private Transaction[] GetGenesisTransactions(Address account, Address tokenAddress)
        {
            var transactions = new List<Transaction>
            {
                GetTransactionForTokenInitialize(account, tokenAddress)
            };

            return transactions.ToArray();
        }

        private Transaction GetTransactionForTokenInitialize(Address account, Address tokenAddress)
        {
            return new Transaction()
            {
                From = account,
                To = tokenAddress,
                MethodName = nameof(ITokenContract.Initialize),
                Params = ByteString.CopyFrom(ParamsPacker.Pack("ELF", "ELF_Token", 100000, 8))
            };
        }

        private async Task<List<Block>> AddBestBranch()
        {
            var bestBranchBlockList = new List<Block>();

            for (var i = 0; i < 10; i++)
            {
                var chain = await _blockchainService.GetChainAsync();
                var transaction = await GenerateTransferTransaction();
                await BroadcastTransaction(new List<Transaction> {transaction});
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
                var transaction = await GenerateTransferTransaction();
                await BroadcastTransaction(new List<Transaction> {transaction});
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