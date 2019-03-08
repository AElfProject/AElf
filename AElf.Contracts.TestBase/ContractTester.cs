using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Authorization;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Dividends;
using AElf.Contracts.Genesis;
using AElf.Contracts.Resource;
using AElf.Contracts.Resource.FeeReceiver;
using AElf.Contracts.Token;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Services;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Network;
using AElf.OS.Node.Application;
using AElf.Types.CSharp;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Contracts.TestBase
{
    public class ContractTester : ITransientDependency
    {
        private readonly int _chainId;
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly IBlockGenerationService _blockGenerationService;
        private ISystemTransactionGenerationService _systemTransactionGenerationService;
        private readonly IBlockExecutingService _blockExecutingService;
        private readonly IConsensusService _consensusService;
        private readonly IBlockchainExecutingService _blockchainExecutingService;
        private readonly IChainManager _chainManager;
        private readonly ITransactionResultQueryService _transactionResultQueryService;
        private readonly IOsBlockchainNodeContextService _osBlockchainNodeContextService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITransactionManager _transactionManager;

        private IAbpApplicationWithInternalServiceProvider Application { get; set; }
        private readonly IAccountService _accountService;

        public ECKeyPair CallOwnerKeyPair { get; set; }

        public ContractTester(int chainId = 0, ECKeyPair callOwnerKeyPair = null, int portNumber = 0)
        {
            _chainId = (chainId == 0) ? ChainHelpers.ConvertBase58ToChainId("AELF") : chainId;

            CallOwnerKeyPair = callOwnerKeyPair ?? CryptoHelpers.GenerateKeyPair();

            var mockAccountService = new Mock<IAccountService>();
            mockAccountService.Setup(s => s.GetPublicKeyAsync()).ReturnsAsync(CallOwnerKeyPair.PublicKey);
            _accountService = mockAccountService.Object;

            Application =
                AbpApplicationFactory.Create<ContractTestAElfModule>(options =>
                {
                    options.UseAutofac();
                    options.Services.Configure<ChainOptions>(o => { o.ChainId = _chainId; });
                    options.Services.Configure<DPoSOptions>(o =>
                    {
                        var minersKeyPairs = new List<ECKeyPair> {CallOwnerKeyPair};
                        for (var i = 0; i < 2; i++)
                        {
                            minersKeyPairs.Add(CryptoHelpers.GenerateKeyPair());
                        }

                        o.InitialMiners = minersKeyPairs.Select(p => p.PublicKey.ToHex()).ToList();
                        o.MiningInterval = 4000;
                        o.IsBootMiner = true;
                    });
                    //options.Services.AddSingleton(new ServiceDescriptor(typeof(IAccountService), _accountService));
                    options.Services.Configure<NetworkOptions>(o => { o.ListeningPort = portNumber; });
                    options.Services.AddSingleton(Mock.Of<IAccountService>(s =>
                        s.GetPublicKeyAsync() == Task.FromResult(CallOwnerKeyPair.PublicKey)));

                    var mockTxHub = new Mock<ITxHub>();
                    mockTxHub.Setup(h => h.HandleBlockAcceptedAsync(It.IsAny<BlockAcceptedEvent>()))
                        .Returns(Task.CompletedTask);
                    options.Services.AddSingleton(new ServiceDescriptor(typeof(ITxHub), mockTxHub.Object));
                });

            Application.Initialize();

            _blockchainService = Application.ServiceProvider.GetService<IBlockchainService>();
            _transactionReadOnlyExecutionService =
                Application.ServiceProvider.GetService<ITransactionReadOnlyExecutionService>();
            _blockGenerationService = Application.ServiceProvider.GetService<IBlockGenerationService>();
            _blockExecutingService = Application.ServiceProvider.GetService<IBlockExecutingService>();
            _consensusService = Application.ServiceProvider.GetService<IConsensusService>();
            _chainManager = Application.ServiceProvider.GetService<IChainManager>();
            _transactionResultQueryService = Application.ServiceProvider.GetService<ITransactionResultQueryService>();
            _blockchainExecutingService = Application.ServiceProvider.GetService<IBlockchainExecutingService>();
            _osBlockchainNodeContextService =
                Application.ServiceProvider.GetRequiredService<IOsBlockchainNodeContextService>();
            _smartContractAddressService =
                Application.ServiceProvider.GetRequiredService<ISmartContractAddressService>();
            _transactionManager = Application.ServiceProvider.GetRequiredService<ITransactionManager>();
        }

        public void SetCallOwner(ECKeyPair caller)
        {
            CallOwnerKeyPair = caller;
        }

        public Address GetContractAddress(Hash name)
        {
            return name == Hash.FromString(typeof(BasicContractZero).FullName)
                ? _smartContractAddressService.GetZeroSmartContractAddress()
                : _smartContractAddressService.GetAddressByContractName(name);
        }

        public Address GetContractAddress(Type contractType)
        {
            return contractType == typeof(BasicContractZero)
                ? _smartContractAddressService.GetZeroSmartContractAddress()
                : _smartContractAddressService.GetAddressByContractName(Hash.FromString(contractType.FullName));
        }

        public Address GetZeroContractAddress()
        {
            return _smartContractAddressService.GetZeroSmartContractAddress();
        }

        public Address GetCallOwnerAddress()
        {
            return Address.FromPublicKey(CallOwnerKeyPair.PublicKey);
        }

        /// <summary>
        /// Initial a chain with given chain id (passed to ctor),
        /// and produce the genesis block with provided contract types.
        /// </summary>
        /// <param name="contractTypes"></param>
        /// <returns>Return contract addresses as the param order.</returns>
        public async Task InitialChainAsync(params Type[] contractTypes)
        {
            var dto = new OsBlockchainNodeContextStartDto
            {
                ChainId = _chainId,
                ZeroSmartContract = typeof(BasicContractZero)
            };

            dto.InitializationSmartContracts.AddConsensusSmartContract<ConsensusContract>();
            dto.InitializationSmartContracts.AddGenesisSmartContracts(contractTypes);

            await _osBlockchainNodeContextService.StartAsync(dto);
        }

        public Address GetConsensusContractAddress()
        {
            return _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider
                .Name);
        }

        /// <summary>
        /// Generate a transaction and sign it.
        /// </summary>
        /// <param name="contractAddress"></param>
        /// <param name="methodName"></param>
        /// <param name="objects"></param>
        /// <returns></returns>
        public Transaction GenerateTransaction(Address contractAddress, string methodName, params object[] objects)
        {
            var tx = new Transaction
            {
                From = Address.FromPublicKey(CallOwnerKeyPair.PublicKey),
                To = contractAddress,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(objects)),
                RefBlockNumber = _blockchainService.GetBestChainLastBlock().Result.Height,
            };

            var signature = CryptoHelpers.SignWithPrivateKey(CallOwnerKeyPair.PrivateKey, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature));

            return tx;
        }

        /// <summary>
        /// Generate a transaction and sign it by provided key pair.
        /// </summary>
        /// <param name="contractAddress"></param>
        /// <param name="methodName"></param>
        /// <param name="ecKeyPair"></param>
        /// <param name="objects"></param>
        /// <returns></returns>
        public Transaction GenerateTransaction(Address contractAddress, string methodName, ECKeyPair ecKeyPair,
            params object[] objects)
        {
            var tx = new Transaction
            {
                From = Address.FromPublicKey(CallOwnerKeyPair.PublicKey),
                To = contractAddress,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(objects)),
                RefBlockNumber = _blockchainService.GetBestChainLastBlock().Result.Height
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
        /// <param name="systemTxs"></param>
        /// <returns></returns>
        public async Task<Block> MineABlockAsync(List<Transaction> txs, List<Transaction> systemTxs = null)
        {
            var preBlock = await _blockchainService.GetBestChainLastBlock();
            var minerService = BuildMinerService(txs, systemTxs);
            return await minerService.MineAsync(preBlock.GetHash(), preBlock.Height,
                DateTime.UtcNow.AddMilliseconds(4000));
        }

        /// <summary>
        /// Generate a tx then package the new tx to a new block.
        /// </summary>
        /// <param name="contractAddress"></param>
        /// <param name="methodName"></param>
        /// <param name="objects"></param>
        /// <returns></returns>
        public async Task<TransactionResult> ExecuteContractWithMiningAsync(Address contractAddress, string methodName,
            params object[] objects)
        {
            var tx = GenerateTransaction(contractAddress, methodName, objects);
            await MineABlockAsync(new List<Transaction> {tx});
            var result = await GetTransactionResult(tx.GetHash());

            return result;
        }

        /// <summary>
        /// Using tx to call a method without mining.
        /// The state database won't change.
        /// </summary>
        /// <param name="contractAddress"></param>
        /// <param name="methodName"></param>
        /// <param name="objects"></param>
        /// <returns></returns>
        public async Task<ByteString> CallContractMethodAsync(Address contractAddress, string methodName,
            params object[] objects)
        {
            var tx = GenerateTransaction(contractAddress, methodName, objects);
            var preBlock = await _blockchainService.GetBestChainLastBlock();
            var transactionTrace = await _transactionReadOnlyExecutionService.ExecuteAsync(new ChainContext
                {
                    BlockHash = preBlock.GetHash(),
                    BlockHeight = preBlock.Height
                },
                tx,
                DateTime.UtcNow);

            return transactionTrace.RetVal?.Data;
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

        public async Task<Chain> GetChainAsync()
        {
            return await _blockchainService.GetChainAsync();
        }

        /// <summary>
        /// Execute a block and add it to chain database.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="txs"></param>
        /// <param name="systemTxs"></param>
        /// <returns></returns>
        public async Task ExecuteBlock(Block block, List<Transaction> txs, List<Transaction> systemTxs)
        {
            txs.ForEach(tx => AsyncHelper.RunSync(() => _transactionManager.AddTransactionAsync(tx)));
            systemTxs.ForEach(tx => AsyncHelper.RunSync(() => _transactionManager.AddTransactionAsync(tx)));
            await _blockchainService.AddBlockAsync(block);
            var chain = await _blockchainService.GetChainAsync();
            var status = await _blockchainService.AttachBlockToChainAsync(chain, block);
            await _blockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);
        }

        /// <summary>
        /// Get the execution result of a tx by its tx id.
        /// </summary>
        /// <param name="txId"></param>
        /// <returns></returns>
        public async Task<TransactionResult> GetTransactionResult(Hash txId)
        {
            return await _transactionResultQueryService.GetTransactionResultAsync(txId);
        }

        private MinerService BuildMinerService(List<Transaction> txs, List<Transaction> systemTxs = null)
        {
            var trs = new List<TransactionReceipt>();

            foreach (var transaction in txs)
            {
                var tr = new TransactionReceipt(transaction)
                {
                    SignatureStatus = SignatureStatus.SignatureValid, RefBlockStatus = RefBlockStatus.RefBlockValid
                };
                trs.Add(tr);
            }

            var bcs = _blockchainService;
            var mockTxHub = new Mock<ITxHub>();
            mockTxHub.Setup(h => h.GetExecutableTransactionSetAsync()).ReturnsAsync(() =>
            {
                var chain = bcs.GetChainAsync().Result;
                return new ExecutableTransactionSet()
                {
                    PreviousBlockHash = chain.BestChainHash,
                    PreviousBlockHeight = chain.BestChainHeight,
                    Transactions = txs
                };
            });
            mockTxHub.Setup(h => h.HandleBlockAcceptedAsync(It.IsAny<BlockAcceptedEvent>()))
                .Returns(Task.CompletedTask);

            var mockSystemTransactionGenerationService = new Mock<ISystemTransactionGenerationService>();

            if (systemTxs != null)
            {
                mockSystemTransactionGenerationService.Setup(s =>
                    s.GenerateSystemTransactions(It.IsAny<Address>(), It.IsAny<long>(), It.IsAny<Hash>()
                    )).Returns(systemTxs);
            }
            else
            {
                mockSystemTransactionGenerationService.Setup(s =>
                    s.GenerateSystemTransactions(It.IsAny<Address>(), It.IsAny<long>(), It.IsAny<Hash>()
                    )).Returns(new List<Transaction>());
            }
            
            _systemTransactionGenerationService = mockSystemTransactionGenerationService.Object;

            return new MinerService(_accountService, _blockGenerationService,
                _systemTransactionGenerationService, _blockchainService, _blockExecutingService, _consensusService,
                _blockchainExecutingService, mockTxHub.Object);
        }

        public Address GetAddress(ECKeyPair keyPair)
        {
            return Address.FromPublicKey(keyPair.PublicKey);
        }

        /// <summary>
        /// Zero Contract and Consensus Contract will deploy independently, thus this list won't contain this two contracts.
        /// </summary>
        /// <returns></returns>
        public List<Type> GetDefaultContractTypes()
        {
            return new List<Type>
            {
                typeof(TokenContract),
                typeof(CrossChainContract),
                typeof(AuthorizationContract),
                typeof(ResourceContract),
                typeof(DividendsContract),
                typeof(FeeReceiverContract)
            };
        }
    }
}