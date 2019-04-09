using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.Authorization;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Dividend;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Resource;
using AElf.Contracts.Resource.FeeReceiver;
using AElf.CrossChain;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.Token;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Node.Application;
using AElf.OS.Node.Domain;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Contracts.TestBase
{
    /// <summary>
    /// For testing contracts.
    /// Basically we can use this class to:
    /// 
    /// Create a new chain with provided smart contracts deployed,
    /// Package chosen transactions and mine a block,
    /// execute a block if transactions in block body provided,
    /// call a contract method and get the execution result,
    /// generate a new tester using exists context,
    /// etc.
    /// </summary>
    /// <typeparam name="TContractTestAElfModule"></typeparam>
    //[Obsolete("Deprecated. Use AElf.Contracts.TestKit for contract testing.")]
    public class ContractTester<TContractTestAElfModule> : ITransientDependency
        where TContractTestAElfModule : ContractTestAElfModule
    {
        private IAbpApplicationWithInternalServiceProvider Application { get; }

        public ECKeyPair KeyPair { get; }

        public string PublicKey => KeyPair.PublicKey.ToHex();

        public long TokenTotalSupply = 1000_000L;
        public long InitialDividendToken = 200_000L;
        public long InitialBalanceOfStarter = 800_000L;

        public ContractTester() : this(0, null)
        {
        }

        public ContractTester(int chainId, ECKeyPair keyPair)
        {
            KeyPair = keyPair ?? CryptoHelpers.GenerateKeyPair();

            Application =
                AbpApplicationFactory.Create<TContractTestAElfModule>(options =>
                {
                    options.UseAutofac();
                    if (chainId != 0)
                    {
                        options.Services.Configure<ChainOptions>(o => { o.ChainId = chainId; });
                    }

                    if (keyPair != null)
                    {
                        options.Services.AddTransient(o =>
                        {
                            var mockService = new Mock<IAccountService>();
                            mockService.Setup(a => a.SignAsync(It.IsAny<byte[]>())).Returns<byte[]>(data =>
                                Task.FromResult(CryptoHelpers.SignWithPrivateKey(KeyPair.PrivateKey, data)));

                            mockService.Setup(a => a.VerifySignatureAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>(),
                                It.IsAny<byte[]>()
                            )).Returns<byte[], byte[], byte[]>((signature, data, publicKey) =>
                            {
                                var recoverResult =
                                    CryptoHelpers.RecoverPublicKey(signature, data, out var recoverPublicKey);
                                return Task.FromResult(recoverResult && publicKey.BytesEqual(recoverPublicKey));
                            });

                            mockService.Setup(a => a.GetPublicKeyAsync()).ReturnsAsync(KeyPair.PublicKey);

                            return mockService.Object;
                        });
                    }
                });

            Application.Initialize();
        }

        private ContractTester(IAbpApplicationWithInternalServiceProvider application, ECKeyPair keyPair)
        {
            application.Services.AddTransient(o =>
            {
                var mockService = new Mock<IAccountService>();
                mockService.Setup(a => a.SignAsync(It.IsAny<byte[]>())).Returns<byte[]>(data =>
                    Task.FromResult(CryptoHelpers.SignWithPrivateKey(keyPair.PrivateKey, data)));

                mockService.Setup(a => a.VerifySignatureAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>()
                )).Returns<byte[], byte[], byte[]>((signature, data, publicKey) =>
                {
                    var recoverResult = CryptoHelpers.RecoverPublicKey(signature, data, out var recoverPublicKey);
                    return Task.FromResult(recoverResult && publicKey.BytesEqual(recoverPublicKey));
                });

                mockService.Setup(a => a.GetPublicKeyAsync()).ReturnsAsync(keyPair.PublicKey);

                return mockService.Object;
            });

            Application = application;

            KeyPair = keyPair;
        }

        private ContractTester(IAbpApplicationWithInternalServiceProvider application, int chainId)
        {
            application.Services.Configure<ChainOptions>(o => { o.ChainId = chainId; });

            Application = application;
        }

        /// <summary>
        /// Use default chain id.
        /// </summary>
        /// <param name="keyPair"></param>
        public ContractTester(ECKeyPair keyPair)
        {
            Application =
                AbpApplicationFactory.Create<TContractTestAElfModule>(options =>
                {
                    options.UseAutofac();
                    options.Services.AddTransient(o =>
                    {
                        var mockService = new Mock<IAccountService>();
                        mockService.Setup(a => a.SignAsync(It.IsAny<byte[]>())).Returns<byte[]>(data =>
                            Task.FromResult(CryptoHelpers.SignWithPrivateKey(keyPair.PrivateKey, data)));

                        mockService.Setup(a => a.VerifySignatureAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>(),
                            It.IsAny<byte[]>()
                        )).Returns<byte[], byte[], byte[]>((signature, data, publicKey) =>
                        {
                            var recoverResult =
                                CryptoHelpers.RecoverPublicKey(signature, data, out var recoverPublicKey);
                            return Task.FromResult(recoverResult && publicKey.BytesEqual(recoverPublicKey));
                        });

                        mockService.Setup(a => a.GetPublicKeyAsync()).ReturnsAsync(keyPair.PublicKey);

                        return mockService.Object;
                    });
                });

            Application.Initialize();

            KeyPair = keyPair;
        }

        /// <summary>
        /// Initial a chain with given chain id (passed to ctor),
        /// and produce the genesis block with provided smart contract configuration.
        /// Will deploy consensus contract by default.
        /// </summary>
        /// <returns>Return contract addresses as the param order.</returns>
        public async Task<OsBlockchainNodeContext> InitialChainAsync(Action<List<GenesisSmartContractDto>> configureSmartContract = null)
        {
            var osBlockchainNodeContextService =
                Application.ServiceProvider.GetRequiredService<IOsBlockchainNodeContextService>();
            var chainOptions = Application.ServiceProvider.GetService<IOptionsSnapshot<ChainOptions>>().Value;
            var dposOptions = Application.ServiceProvider.GetService<IOptionsSnapshot<DPoSOptions>>().Value;
            var dto = new OsBlockchainNodeContextStartDto
            {
                ChainId = chainOptions.ChainId,
                ZeroSmartContract = typeof(BasicContractZero),
                SmartContractRunnerCategory = SmartContractTestConstants.TestRunnerCategory
            };

            dto.InitializationSmartContracts.AddConsensusSmartContract<ConsensusContract>(
                GenerateConsensusInitializationCallList(dposOptions));
            configureSmartContract?.Invoke(dto.InitializationSmartContracts);

            return await osBlockchainNodeContextService.StartAsync(dto);
        }
        
        public async Task<OsBlockchainNodeContext> InitialCustomizedChainAsync(List<string> initialMiners = null, int miningInterval = 4000,
            Timestamp startTimestamp = null, Action<List<GenesisSmartContractDto>> configureSmartContract = null)
        {
            if (initialMiners == null)
            {
                initialMiners = Enumerable.Range(0, 3).Select(i => CryptoHelpers.GenerateKeyPair().PublicKey.ToHex())
                    .ToList();
            }

            if (startTimestamp == null)
            {
                startTimestamp = DateTime.UtcNow.ToTimestamp();
            }
            
            var osBlockchainNodeContextService =
                Application.ServiceProvider.GetRequiredService<IOsBlockchainNodeContextService>();
            var chainOptions = Application.ServiceProvider.GetService<IOptionsSnapshot<ChainOptions>>().Value;
            var dto = new OsBlockchainNodeContextStartDto
            {
                ChainId = chainOptions.ChainId,
                ZeroSmartContract = typeof(BasicContractZero),
                SmartContractRunnerCategory = SmartContractTestConstants.TestRunnerCategory
            };

            dto.InitializationSmartContracts.AddConsensusSmartContract<ConsensusContract>(
                GenerateConsensusInitializationCallList(initialMiners, miningInterval, startTimestamp));
            configureSmartContract?.Invoke(dto.InitializationSmartContracts);

            return await osBlockchainNodeContextService.StartAsync(dto);
        }

        private SystemTransactionMethodCallList GenerateConsensusInitializationCallList(DPoSOptions dposOptions)
        {
            var consensusMethodCallList = new SystemTransactionMethodCallList();
            consensusMethodCallList.Add(nameof(ConsensusContract.InitialDPoSContract),
                new InitialDPoSContractInput
                {
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                    DividendsContractSystemName = DividendsSmartContractAddressNameProvider.Name
                });
            consensusMethodCallList.Add(nameof(ConsensusContract.InitialConsensus),
                dposOptions.InitialMiners.ToMiners().GenerateFirstRoundOfNewTerm(dposOptions.MiningInterval,
                    DateTime.Parse(dposOptions.StartTimestamp).ToUniversalTime()));
            return consensusMethodCallList;
        }
        
        private SystemTransactionMethodCallList GenerateConsensusInitializationCallList(List<string> initialMiners,
            int miningInterval, Timestamp startTimestamp)
        {
            var consensusMethodCallList = new SystemTransactionMethodCallList();
            consensusMethodCallList.Add(nameof(ConsensusContract.InitialDPoSContract),
                new InitialDPoSContractInput
                {
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                    DividendsContractSystemName = DividendsSmartContractAddressNameProvider.Name
                });
            consensusMethodCallList.Add(nameof(ConsensusContract.InitialConsensus),
                initialMiners.ToMiners().GenerateFirstRoundOfNewTerm(miningInterval, startTimestamp.ToDateTime()));
            return consensusMethodCallList;
        }

        public async Task InitialSideChainAsync(Action<List<GenesisSmartContractDto>> configureSmartContract = null)
        {
            var osBlockchainNodeContextService =
                Application.ServiceProvider.GetRequiredService<IOsBlockchainNodeContextService>();
            var chainOptions = Application.ServiceProvider.GetService<IOptionsSnapshot<ChainOptions>>().Value;
            var dto = new OsBlockchainNodeContextStartDto
            {
                ChainId = chainOptions.ChainId,
                ZeroSmartContract = typeof(BasicContractZero),
                SmartContractRunnerCategory = SmartContractTestConstants.TestRunnerCategory
            };

            dto.InitializationSmartContracts
                .AddConsensusSmartContract<AElf.Contracts.Consensus.DPoS.SideChain.ConsensusContract>();
            configureSmartContract?.Invoke(dto.InitializationSmartContracts);

            await osBlockchainNodeContextService.StartAsync(dto);
        }

        /// <summary>
        /// Use randomized ECKeyPair.
        /// </summary>
        /// <param name="chainId"></param>
        public ContractTester(int chainId)
        {
            Application =
                AbpApplicationFactory.Create<TContractTestAElfModule>(options =>
                {
                    options.UseAutofac();
                    options.Services.Configure<ChainOptions>(o => { o.ChainId = chainId; });
                });
            Application.Initialize();
        }

        /// <summary>
        /// Same chain, different key pair.
        /// </summary>
        /// <param name="keyPair"></param>
        /// <returns></returns>
        public ContractTester<TContractTestAElfModule> CreateNewContractTester(ECKeyPair keyPair)
        {
            return new ContractTester<TContractTestAElfModule>(Application, keyPair);
        }

        /// <summary>
        /// Same key pair, different chain.
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        public ContractTester<TContractTestAElfModule> CreateNewContractTester(int chainId)
        {
            return new ContractTester<TContractTestAElfModule>(Application, chainId);
        }

        public async Task<byte[]> GetPublicKeyAsync()
        {
            var accountService = Application.ServiceProvider.GetRequiredService<IAccountService>();
            return await accountService.GetPublicKeyAsync();
        }

        public Address GetContractAddress(Hash name)
        {
            var smartContractAddressService =
                Application.ServiceProvider.GetRequiredService<ISmartContractAddressService>();
            return name == Hash.Empty
                ? smartContractAddressService.GetZeroSmartContractAddress()
                : smartContractAddressService.GetAddressByContractName(name);
        }


        public Address GetZeroContractAddress()
        {
            var smartContractAddressService =
                Application.ServiceProvider.GetRequiredService<ISmartContractAddressService>();
            return smartContractAddressService.GetZeroSmartContractAddress();
        }

        public Address GetConsensusContractAddress()
        {
            var smartContractAddressService =
                Application.ServiceProvider.GetRequiredService<ISmartContractAddressService>();
            return smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider
                .Name);
        }

        public Address GetCallOwnerAddress()
        {
            return Address.FromPublicKey(KeyPair.PublicKey);
        }

        public async Task<Transaction> GenerateTransactionAsync(Address contractAddress, string methodName,
            IMessage input)
        {
            var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var refBlock = await blockchainService.GetBestChainLastBlockHeaderAsync();
            var tx = new Transaction
            {
                From = Address.FromPublicKey(KeyPair.PublicKey),
                To = contractAddress,
                MethodName = methodName,
                Params = input.ToByteString(),
                RefBlockNumber = refBlock.Height,
                RefBlockPrefix = ByteString.CopyFrom(refBlock.GetHash().Value.Take(4).ToArray())
            };

            var signature = CryptoHelpers.SignWithPrivateKey(KeyPair.PrivateKey, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature));

            return tx;
        }

        /// <summary>
        /// Generate a transaction and sign it by provided key pair.
        /// </summary>
        /// <param name="contractAddress"></param>
        /// <param name="methodName"></param>
        /// <param name="ecKeyPair"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<Transaction> GenerateTransactionAsync(Address contractAddress, string methodName,
            ECKeyPair ecKeyPair, IMessage input)
        {
            var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var refBlock = await blockchainService.GetBestChainLastBlockHeaderAsync();
            var paramInfo =
                input == null ? ByteString.Empty : input.ToByteString(); //Add input parameter is null situation
            var tx = new Transaction
            {
                From = Address.FromPublicKey(ecKeyPair.PublicKey),
                To = contractAddress,
                MethodName = methodName,
                Params = paramInfo,
                RefBlockNumber = refBlock.Height,
                RefBlockPrefix = ByteString.CopyFrom(refBlock.GetHash().Value.Take(4).ToArray())
            };

            var signature = CryptoHelpers.SignWithPrivateKey(ecKeyPair.PrivateKey, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature));

            return tx;
        }

        /// <summary>
        /// Mine a block with given normal txs and system txs.
        /// Normal txs will use tx pool while system txs not.
        /// </summary>
        /// <param name="txs"></param>
        /// <returns></returns>
        public async Task<Block> MineAsync(List<Transaction> txs)
        {
            await AddTransactionsAsync(txs);
            var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var preBlock = await blockchainService.GetBestChainLastBlockHeaderAsync();
            var minerService = Application.ServiceProvider.GetRequiredService<IMinerService>();
            var blockAttachService = Application.ServiceProvider.GetRequiredService<IBlockAttachService>();

            var block = await minerService.MineAsync(preBlock.GetHash(), preBlock.Height,
                DateTime.UtcNow, TimeSpan.FromMilliseconds(int.MaxValue));
            
            await blockAttachService.AttachBlockAsync(block);
    
            return block;
        }

        /// <summary>
        /// In test cases, we can't distinguish normal txs and system txs,
        /// so just keep in mind if some txs looks like system txs,
        /// just add them first manually.
        /// </summary>
        /// <param name="txs"></param>
        /// <returns></returns>
        private async Task AddTransactionsAsync(IEnumerable<Transaction> txs)
        {
            var txHub = Application.ServiceProvider.GetRequiredService<ITxHub>();
            foreach (var tx in txs)
            {
                await txHub.HandleTransactionsReceivedAsync(new TransactionsReceivedEvent
                {
                    Transactions = new List<Transaction> {tx}
                });
            }
        }

        /// <summary>
        /// Generate a tx then package the new tx to a new block.
        /// </summary>
        /// <param name="contractAddress"></param>
        /// <param name="methodName"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<TransactionResult> ExecuteContractWithMiningAsync(Address contractAddress, string methodName,
            IMessage input)
        {
            var tx = await GenerateTransactionAsync(contractAddress, methodName, KeyPair, input);
            await MineAsync(new List<Transaction> {tx});
            var result = await GetTransactionResultAsync(tx.GetHash());

            return result;
        }

        /// <summary>
        ///  Generate a tx then package the new tx to a new block.
        /// </summary>
        /// <param name="contractAddress"></param>
        /// <param name="methodName"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<(Block, Transaction)> ExecuteContractWithMiningReturnBlockAsync(Address contractAddress,
            string methodName, IMessage input)
        {
            var tx = await GenerateTransactionAsync(contractAddress, methodName, KeyPair, input);
            return (await MineAsync(new List<Transaction> {tx}), tx);
        }

        /// <summary>
        /// Using tx to call a method without mining.
        /// The state database won't change.
        /// </summary>
        /// <param name="contractAddress"></param>
        /// <param name="methodName"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<ByteString> CallContractMethodAsync(Address contractAddress, string methodName,
            IMessage input)
        {
            var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var transactionReadOnlyExecutionService =
                Application.ServiceProvider.GetRequiredService<ITransactionReadOnlyExecutionService>();
            var tx = await GenerateTransactionAsync(contractAddress, methodName, input);
            var preBlock = await blockchainService.GetBestChainLastBlockHeaderAsync();
            var transactionTrace = await transactionReadOnlyExecutionService.ExecuteAsync(new ChainContext
                {
                    BlockHash = preBlock.GetHash(),
                    BlockHeight = preBlock.Height
                }, tx, DateTime.UtcNow);

            return transactionTrace.ReturnValue;
        }
        
        public async Task<ByteString> CallContractMethodAsync(Address contractAddress, string methodName,
            IMessage input, DateTime dateTime)
        {
            var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var transactionReadOnlyExecutionService =
                Application.ServiceProvider.GetRequiredService<ITransactionReadOnlyExecutionService>();
            var tx = await GenerateTransactionAsync(contractAddress, methodName, input);
            var preBlock = await blockchainService.GetBestChainLastBlockHeaderAsync();
            var transactionTrace = await transactionReadOnlyExecutionService.ExecuteAsync(new ChainContext
            {
                BlockHash = preBlock.GetHash(),
                BlockHeight = preBlock.Height
            }, tx, dateTime);

            return transactionTrace.ReturnValue;
        }

        public void SignTransaction(ref List<Transaction> transactions, ECKeyPair callerKeyPair)
        {
            foreach (var transaction in transactions)
            {
                var signature =
                    CryptoHelpers.SignWithPrivateKey(callerKeyPair.PrivateKey, transaction.GetHash().DumpByteArray());
                transaction.Sigs.Add(ByteString.CopyFrom(signature));
            }
        }

        public void SupplyTransactionParameters(ref List<Transaction> transactions)
        {
            var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var refBlock = AsyncHelper.RunSync(() => blockchainService.GetBestChainLastBlockHeaderAsync());
            foreach (var transaction in transactions)
            {
                transaction.RefBlockNumber = refBlock.Height;
                transaction.RefBlockPrefix = ByteString.CopyFrom(refBlock.GetHash().Value.Take(4).ToArray());
            }
        }

        public async Task<Chain> GetChainAsync()
        {
            var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            return await blockchainService.GetChainAsync();
        }

        /// <summary>
        /// Execute a block and add it to chain database.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="txs"></param>
        /// <returns></returns>
        public async Task ExecuteBlock(Block block, List<Transaction> txs)
        {
            var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var transactionManager = Application.ServiceProvider.GetRequiredService<ITransactionManager>();
            var blockchainExecutingService =
                Application.ServiceProvider.GetRequiredService<IBlockchainExecutingService>();
            txs.ForEach(tx => AsyncHelper.RunSync(() => transactionManager.AddTransactionAsync(tx)));
            await blockchainService.AddBlockAsync(block);
            var chain = await blockchainService.GetChainAsync();
            var status = await blockchainService.AttachBlockToChainAsync(chain, block);
            await blockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);
        }

        /// <summary>
        /// Get the execution result of a tx by its tx id.
        /// </summary>
        /// <param name="txId"></param>
        /// <returns></returns>
        public async Task<TransactionResult> GetTransactionResultAsync(Hash txId)
        {
            var transactionResultQueryService =
                Application.ServiceProvider.GetRequiredService<ITransactionResultQueryService>();
            return await transactionResultQueryService.GetTransactionResultAsync(txId);
        }

        public Address GetAddress(ECKeyPair keyPair)
        {
            return Address.FromPublicKey(keyPair.PublicKey);
        }

        /// <summary>
        /// Zero Contract and Consensus Contract will deploy independently, thus this list won't contain this two contracts.
        /// </summary>
        /// <returns></returns>
        public Action<List<GenesisSmartContractDto>> GetDefaultContractTypes(Address issuer, out long totalSupply,
            out long dividend, out long balanceOfStarter)
        {
            totalSupply = TokenTotalSupply;
            dividend = InitialDividendToken;
            balanceOfStarter = InitialBalanceOfStarter;

            var callList = new SystemTransactionMethodCallList();
            callList.Add(nameof(TokenContract.InitializeTokenContract), new IntializeTokenContractInput
            {
                CrossChainContractSystemName = CrossChainSmartContractAddressNameProvider.Name
            });
            callList.Add(nameof(TokenContract.CreateNativeToken), new CreateNativeTokenInput
            {
                Symbol = "ELF",
                Decimals = 2,
                Issuer = issuer,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = TokenTotalSupply
            });
            callList.Add(nameof(TokenContract.IssueNativeToken), new IssueNativeTokenInput
            {
                Symbol = "ELF",
                Amount = InitialDividendToken,
                ToSystemContractName = DividendsSmartContractAddressNameProvider.Name
            });
            callList.Add(nameof(TokenContract.Issue), new IssueInput
            {
                Symbol = "ELF",
                Amount = InitialBalanceOfStarter,
                To = GetCallOwnerAddress()
            });

            return list =>
            {
                list.AddGenesisSmartContract<DividendContract>(DividendsSmartContractAddressNameProvider.Name);
                //TODO: support initialize method, make the tester auto issue elf token
                list.AddGenesisSmartContract<TokenContract>(TokenSmartContractAddressNameProvider.Name, callList);
                list.AddGenesisSmartContract<ResourceContract>(ResourceSmartContractAddressNameProvider.Name);
                list.AddGenesisSmartContract<FeeReceiverContract>(ResourceFeeReceiverSmartContractAddressNameProvider
                    .Name);
                list.AddGenesisSmartContract<CrossChainContract>(CrossChainSmartContractAddressNameProvider.Name);
            };
        }
    }
}