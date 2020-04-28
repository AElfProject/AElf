using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using AElf.Contracts.Deployer;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.GovernmentSystem;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.Token;
using AElf.Kernel.TransactionPool;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Network;
using AElf.OS.Node.Application;
using AElf.OS.Node.Domain;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Volo.Abp.Threading;

namespace AElf.OS
{
    public class OSTestHelper
    {
        private IReadOnlyDictionary<string, byte[]> _codes;

        public readonly long TokenTotalSupply = 100_0000_0000_0000_0000L;
        public long MockChainTokenAmount { get; private set; }
        public IReadOnlyDictionary<string, byte[]> Codes =>
            _codes ?? (_codes = ContractsDeployer.GetContractCodes<OSTestHelper>());
        public byte[] ConsensusContractCode => Codes.Single(kv => kv.Key.Contains("Consensus.AEDPoS")).Value;
        public byte[] ElectionContractCode => Codes.Single(kv => kv.Key.Contains("Election")).Value;
        public byte[] TokenContractCode =>
            Codes.Single(kv => kv.Key.Split(",").First().Trim().EndsWith("MultiToken")).Value;

        private readonly ChainOptions _chainOptions;
        
        private readonly IOsBlockchainNodeContextService _osBlockchainNodeContextService;
        private readonly IAccountService _accountService;
        private readonly IMinerService _minerService;
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITxHub _txHub;
        private readonly IStaticChainInformationProvider _staticChainInformationProvider;
        private readonly IBlockAttachService _blockAttachService;
        private readonly ITransactionResultService _transactionResultService;
        
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
            ITransactionResultService transactionResultService,
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
            _transactionResultService = transactionResultService;

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
        public async Task MockChainAsync()
        {
            await StartNodeAsync();
            var chain = await _blockchainService.GetChainAsync();

            if (chain.BestChainHeight == 1)
            {
                var genesisBlock = await _blockchainService.GetBlockByHashAsync(chain.GenesisBlockHash);
                BestBranchBlockList.Add(genesisBlock);

                BestBranchBlockList.AddRange(await AddBestBranch());

                ForkBranchBlockList =
                    await AddForkBranch(BestBranchBlockList[4].GetHash(), BestBranchBlockList[4].Height);

                UnlinkedBranchBlockList = await AddForkBranch(HashHelper.ComputeFrom("UnlinkBlock"), 9);

                // Set lib
                chain = await _blockchainService.GetChainAsync();
                await _blockchainService.SetIrreversibleBlockAsync(chain, BestBranchBlockList[4].Height,
                    BestBranchBlockList[4].GetHash());
            }

            await _txHub.HandleBestChainFoundAsync(new BestChainFoundEventData
            {
                 BlockHash = chain.BestChainHash,
                 BlockHeight = chain.BestChainHeight
            });
        }

        public async Task DisposeMock()
        {
            await StopNode();
        }

        public async Task<Transaction> GenerateTransferTransaction()
        {
            var newUserKeyPair = CryptoHelper.GenerateKeyPair();
            var accountAddress = await _accountService.GetAccountAsync();
            
            var transaction = GenerateTransaction(accountAddress,
                await _smartContractAddressService.GetAddressByContractNameAsync(await GetChainContextAsync(), TokenSmartContractAddressNameProvider.StringName),
                nameof(TokenContractContainer.TokenContractStub.Transfer),
                new TransferInput {To = Address.FromPublicKey(newUserKeyPair.PublicKey), Amount = 10, Symbol = "ELF"});

            var signature = await _accountService.SignAsync(transaction.GetHash().ToByteArray());
            transaction.Signature = ByteString.CopyFrom(signature);

            return transaction;
        }

        public async Task<(List<Transaction>, List<ECKeyPair>)> PrepareTokenForParallel(int count, long tokenAmount = 10)
        {
            var transactions = new List<Transaction>();
            var keyPairs = new List<ECKeyPair>();
            
            var accountAddress = await _accountService.GetAccountAsync();
            for (var i = 0; i < count; i++)
            {
                var newUserKeyPair = CryptoHelper.GenerateKeyPair();
                var transaction = GenerateTransaction(accountAddress,
                    await _smartContractAddressService.GetAddressByContractNameAsync(await GetChainContextAsync(), TokenSmartContractAddressNameProvider.StringName),
                    nameof(TokenContractContainer.TokenContractStub.Transfer),
                    new TransferInput {To = Address.FromPublicKey(newUserKeyPair.PublicKey), Amount = tokenAmount, Symbol = "ELF"});

                var signature = await _accountService.SignAsync(transaction.GetHash().ToByteArray());
                transaction.Signature = ByteString.CopyFrom(signature);

                transactions.Add(transaction);
                keyPairs.Add(newUserKeyPair);
            }

            return (transactions, keyPairs);
        }

        public async Task<List<Transaction>> GenerateTransactionsWithoutConflictAsync(List<ECKeyPair> keyPairs, int count = 1)
        {
            var transactions = new List<Transaction>();
            foreach (var keyPair in keyPairs)
            {
                var from = Address.FromPublicKey(keyPair.PublicKey);
                for (var i = 0; i < count; i++)
                {
                    var to = CryptoHelper.GenerateKeyPair();
                    var transaction = GenerateTransaction(from,
                        await _smartContractAddressService.GetAddressByContractNameAsync( await GetChainContextAsync(), TokenSmartContractAddressNameProvider.StringName),
                        nameof(TokenContractContainer.TokenContractStub.Transfer),
                        new TransferInput {To = Address.FromPublicKey(to.PublicKey), Amount = 1, Symbol = "ELF"});                   
                    var signature = CryptoHelper.SignWithPrivateKey(keyPair.PrivateKey, transaction.GetHash().ToByteArray());
                    transaction.Signature = ByteString.CopyFrom(signature); 

                    transactions.Add(transaction);
                }
            }

            return transactions;
        }
        
        public async Task<List<Transaction>> GenerateApproveTransactions(List<ECKeyPair> keyPairs, int count = 1)
        {
            var transactions = new List<Transaction>();
            var spender = await _accountService.GetAccountAsync();
            foreach (var keyPair in keyPairs)
            {
                var from = Address.FromPublicKey(keyPair.PublicKey);
                var transaction = GenerateTransaction(from,
                    await _smartContractAddressService.GetAddressByContractNameAsync(await GetChainContextAsync(), TokenSmartContractAddressNameProvider.StringName),
                    nameof(TokenContractContainer.TokenContractStub.Approve),
                    new ApproveInput {Spender = spender, Amount = count, Symbol = "ELF"});                
                var signature = CryptoHelper.SignWithPrivateKey(keyPair.PrivateKey, transaction.GetHash().ToByteArray());
                transaction.Signature = ByteString.CopyFrom(signature); 

                transactions.Add(transaction);
            }

            return transactions;
        }
        
        public async Task<List<Transaction>> GenerateTransferFromTransactionsWithoutConflict(List<ECKeyPair> keyPairs, int count = 1)
        {
            var transactions = new List<Transaction>();
            var address = await _accountService.GetAccountAsync();
            foreach (var keyPair in keyPairs)
            {
                var from = Address.FromPublicKey(keyPair.PublicKey);
                for (var i = 0; i < count; i++)
                {
                    var to = CryptoHelper.GenerateKeyPair();
                    var transaction = GenerateTransaction(address,
                        await _smartContractAddressService.GetAddressByContractNameAsync(await GetChainContextAsync(), TokenSmartContractAddressNameProvider.StringName),
                        nameof(TokenContractContainer.TokenContractStub.TransferFrom),
                        new TransferFromInput
                            {From = from, To = Address.FromPublicKey(to.PublicKey), Amount = 1, Symbol = "ELF"});                  
                    var signature = await _accountService.SignAsync(transaction.GetHash().ToByteArray());
                    transaction.Signature = ByteString.CopyFrom(signature); 

                    transactions.Add(transaction);
                }
            }

            return transactions;
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

        public Transaction GenerateTransaction(Address from, Address to, string methodName, IMessage input)
        {
            var chain = _blockchainService.GetChainAsync().Result;
            var transaction = new Transaction
            {
                From = from,
                To = to,
                MethodName = methodName,
                Params = input.ToByteString(),
                RefBlockNumber = chain.BestChainHeight,
                RefBlockPrefix = BlockHelper.GetRefBlockPrefix(chain.BestChainHash),
            };

            return transaction;
        }

        public async Task BroadcastTransactions(IEnumerable<Transaction> transactions)
        {
            var transactionsReceivedEvent = new TransactionsReceivedEvent
            {
                Transactions = transactions
            };

            await _txHub.AddTransactionsAsync(transactionsReceivedEvent);
        }

        public async Task<Block> MinedOneBlock(Hash previousBlockHash = null, long previousBlockHeight = 0)
        {
            if (previousBlockHash == null || previousBlockHeight == 0)
            {
                var chain = await _blockchainService.GetChainAsync();
                previousBlockHash = chain.BestChainHash;
                previousBlockHeight = chain.BestChainHeight;
            }

            var block = (await _minerService.MineAsync(previousBlockHash, previousBlockHeight,
                TimestampHelper.GetUtcNow(), TimestampHelper.DurationFromMilliseconds(4000))).Block;

            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);
                
            return block;
        }

        public virtual Block GenerateBlock(Hash preBlockHash, long preBlockHeight, IEnumerable<Transaction> transactions = null)
        {
            var block = new Block
            {
                Header = new BlockHeader
                {
                    ChainId = _staticChainInformationProvider.ChainId,
                    Height = preBlockHeight + 1,
                    PreviousBlockHash = preBlockHash,
                    Time = TimestampHelper.GetUtcNow(),
                    MerkleTreeRootOfTransactions = Hash.Empty,
                    MerkleTreeRootOfWorldState = Hash.Empty,
                    MerkleTreeRootOfTransactionStatus = Hash.Empty,
                    SignerPubkey = ByteString.CopyFrom(AsyncHelper.RunSync(_accountService.GetPublicKeyAsync))
                },
                Body = new BlockBody()
            };
            if (transactions != null)
            {
                foreach (var transaction in transactions)
                {
                    block.AddTransaction(transaction);
                }

                block.Header.MerkleTreeRootOfTransactions = block.Body.CalculateMerkleTreeRoot();
            }

            return block;
        }

        public BlockWithTransactions GenerateBlockWithTransactions(Hash preBlockHash, long preBlockHeight,
            IEnumerable<Transaction> transactions = null)
        {
            var block = GenerateBlock(preBlockHash, preBlockHeight, transactions);
            var blockWithTransactions = new BlockWithTransactions
            {
                Header = block.Header
            };
            if (transactions != null)
            {
                blockWithTransactions.Transactions.AddRange(transactions);
            }

            return blockWithTransactions;
        }

        public async Task<Address> DeployContract<T>()
        {
            var basicContractZero = _smartContractAddressService.GetZeroSmartContractAddress();
            var accountAddress = await _accountService.GetAccountAsync();
            
            var transaction = GenerateTransaction(accountAddress, basicContractZero,
                nameof(BasicContractZeroContainer.BasicContractZeroBase.DeploySmartContract), new ContractDeploymentInput()
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(T).Assembly.Location))
                });

            var signature = await _accountService.SignAsync(transaction.GetHash().ToByteArray());
            transaction.Signature = ByteString.CopyFrom(signature);

            await BroadcastTransactions(new List<Transaction> {transaction});
            await MinedOneBlock();

            var txResult = await _transactionResultService.GetTransactionResultAsync(transaction.GetHash());

            return Address.Parser.ParseFrom(txResult.ReturnValue);
        }

        public async Task<TransactionResult> GetTransactionResultsAsync(Hash transactionId)
        {
            var res = await _transactionResultService.GetTransactionResultAsync(transactionId);
            return res;
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

        private async Task StartNodeAsync()
        {
            var dto = new OsBlockchainNodeContextStartDto
            {
                ZeroSmartContract = typeof(BasicContractZero),
                ChainId = _chainOptions.ChainId
            };

            dto.SmartContractRunnerCategory = KernelConstants.CodeCoverageRunnerCategory;
            dto.InitializationSmartContracts.AddGenesisSmartContract(
                ConsensusContractCode,
                ConsensusSmartContractAddressNameProvider.Name);

            var ownAddress = await _accountService.GetAccountAsync();
            var callList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            callList.Add(nameof(TokenContractContainer.TokenContractStub.Create), new CreateInput
            {
                Symbol = "ELF",
                TokenName = "ELF_Token",
                TotalSupply = TokenTotalSupply,
                Decimals = 2,
                Issuer =  ownAddress,
                IsBurnable = true
            });
            callList.Add(nameof(TokenContractContainer.TokenContractStub.SetPrimaryTokenSymbol),
                new SetPrimaryTokenSymbolInput {Symbol = "ELF"});
            callList.Add(nameof(TokenContractContainer.TokenContractStub.Issue), new IssueInput
            {
                Symbol = "ELF",
                Amount = TokenTotalSupply,
                To = ownAddress,
                Memo = "Issue"
            });
            callList.Add(nameof(TokenContractContainer.TokenContractStub.InitialCoefficients), new Empty());
            
            dto.InitializationSmartContracts.AddGenesisSmartContract(
                ElectionContractCode,
                ElectionSmartContractAddressNameProvider.Name);
            dto.InitializationSmartContracts.AddGenesisSmartContract(
                TokenContractCode,
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
                var transactionResult = await _transactionResultService.GetTransactionResultAsync(transaction.GetHash());
                var relatedLog = transactionResult.Logs.FirstOrDefault(l => l.Name == nameof(TransactionFeeCharged));
                var fee = relatedLog == null ? 0 : TransactionFeeCharged.Parser.ParseFrom(relatedLog.NonIndexed).Amount;
                MockChainTokenAmount += fee + TransferInput.Parser.ParseFrom(transaction.Params).Amount;
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