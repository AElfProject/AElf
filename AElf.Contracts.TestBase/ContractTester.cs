using System;
using System.Collections.Generic;
using System.IO;
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
using AElf.Database;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Node.Application;
using AElf.Kernel.Node.Domain;
using AElf.Kernel.Services;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Network;
using AElf.OS.Node.Application;
using AElf.Types.CSharp;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        private readonly IBlockchainNodeContextService _blockchainNodeContextService;
        private readonly IBlockGenerationService _blockGenerationService;
        private ISystemTransactionGenerationService _systemTransactionGenerationService;
        private readonly IBlockExecutingService _blockExecutingService;
        private readonly IConsensusService _consensusService;
        private readonly IBlockchainExecutingService _blockchainExecutingService;
        private readonly IChainManager _chainManager;
        private readonly ITransactionResultQueryService _transactionResultQueryService;
        private readonly IOsBlockchainNodeContextService _osBlockchainNodeContextService;

        private readonly IAccountService _accountService;

        public Chain Chain => AsyncHelper.RunSync(GetChainAsync);

        public ECKeyPair CallOwnerKeyPair { get; set; }

        public List<Address> DeployedContractsAddresses { get; set; }

        public ContractTester(int chainId = 0, ECKeyPair callOwnerKeyPair = null, int portNumber = 0)
        {
            _chainId = (chainId == 0) ? ChainHelpers.ConvertBase58ToChainId("AELF") : chainId;

            CallOwnerKeyPair = callOwnerKeyPair ?? CryptoHelpers.GenerateKeyPair();

            var mockAccountService = new Mock<IAccountService>();
            mockAccountService.Setup(s => s.GetPublicKeyAsync()).ReturnsAsync(CallOwnerKeyPair.PublicKey);
            _accountService = mockAccountService.Object;

            var application =
                AbpApplicationFactory.Create<ContractTestAElfModule>(options =>
                {
                    options.UseAutofac();
                    options.Services.Configure<ChainOptions>(o => { o.ChainId = _chainId; });
                    options.Services.AddSingleton(new ServiceDescriptor(typeof(IAccountService), _accountService));
                    options.Services.Configure<NetworkOptions>(o => { o.ListeningPort = portNumber; });
                    options.Services.AddSingleton<IAccountService>(_accountService);
                });
            application.Initialize();

            _blockchainService = application.ServiceProvider.GetService<IBlockchainService>();
            _transactionReadOnlyExecutionService =
                application.ServiceProvider.GetService<ITransactionReadOnlyExecutionService>();
            _blockchainNodeContextService = application.ServiceProvider.GetService<IBlockchainNodeContextService>();
            _blockGenerationService = application.ServiceProvider.GetService<IBlockGenerationService>();
            _systemTransactionGenerationService =
                application.ServiceProvider.GetService<ISystemTransactionGenerationService>();
            _blockExecutingService = application.ServiceProvider.GetService<IBlockExecutingService>();
            _consensusService = application.ServiceProvider.GetService<IConsensusService>();
            _chainManager = application.ServiceProvider.GetService<IChainManager>();
            _transactionResultQueryService = application.ServiceProvider.GetService<ITransactionResultQueryService>();
            _blockchainExecutingService = application.ServiceProvider.GetService<IBlockchainExecutingService>();
            _osBlockchainNodeContextService =
                application.ServiceProvider.GetRequiredService<IOsBlockchainNodeContextService>();
        }

        public void SetCallOwner(ECKeyPair caller)
        {
            CallOwnerKeyPair = caller;
        }

        public Address GetCallOwnerAddress()
        {
            return GetAddress(CallOwnerKeyPair);
        }

        /// <summary>
        /// Initial a chain with given chain id (passed to ctor),
        /// and produce the genesis block with provided contract types.
        /// </summary>
        /// <param name="contractTypes"></param>
        /// <returns>Return contract addresses as the param order.</returns>
        public async Task<List<Address>> InitialChainAsync(params Type[] contractTypes)
        {
            var transactions = GetGenesisTransactions(_chainId, contractTypes);
            var dto = new OsBlockchainNodeContextStartDto
            {
                ChainId = _chainId,
            };

            dto.InitializationSmartContracts.AddGenesisSmartContracts(contractTypes);

            await _osBlockchainNodeContextService.StartAsync(dto);

            var addresses = new List<Address>();
            for (var i = 0UL; i < (ulong) contractTypes.Length; i++)
            {
                addresses.Add(GetContractAddress(i));
            }

            DeployedContractsAddresses = addresses;

            return addresses;
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
                From = GetAddress(CallOwnerKeyPair),
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
                From = GetAddress(CallOwnerKeyPair),
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

            return transactionTrace.RetVal?.Data ?? ByteString.Empty;
        }

        public void SignTransaction(ref Transaction transaction, ECKeyPair callerKeyPair)
        {
            var signature =
                CryptoHelpers.SignWithPrivateKey(callerKeyPair.PrivateKey, transaction.GetHash().DumpByteArray());
            transaction.Sigs.Add(ByteString.CopyFrom(signature));
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
            block = await _blockExecutingService.ExecuteBlockAsync(block.Header, systemTxs, txs,
                new CancellationToken());
            await _blockchainService.AddBlockAsync(block);
            var chain = await _blockchainService.GetChainAsync();
            var status = await _blockchainService.AttachBlockToChainAsync(chain, block);
            await _blockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);
        }

        public async Task SetIrreversibleBlock(Hash libHash)
        {
            var chain = await _blockchainService.GetChainAsync();
            await _chainManager.SetIrreversibleBlockAsync(chain, libHash);
        }

        public async Task SetIrreversibleBlock(long libHeight)
        {
            var chain = await _blockchainService.GetChainAsync();
            var libHash = (await _blockchainService.GetBlockByHeightAsync(libHeight)).GetHash();
            chain.LastIrreversibleBlockHash = libHash;
            chain.LastIrreversibleBlockHeight = libHeight;
            await _chainManager.SetIrreversibleBlockAsync(chain, libHash);
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

            if (systemTxs != null)
            {
                var mockSystemTransactionGenerationService = new Mock<ISystemTransactionGenerationService>();
                mockSystemTransactionGenerationService.Setup(s =>
                    s.GenerateSystemTransactions(It.IsAny<Address>(), It.IsAny<long>(), It.IsAny<Hash>()
                    )).Returns(systemTxs);
                _systemTransactionGenerationService = mockSystemTransactionGenerationService.Object;
            }

            return new MinerService(_accountService, _blockGenerationService,
                _systemTransactionGenerationService, _blockchainService, _blockExecutingService, _consensusService,
                _blockchainExecutingService, mockTxHub.Object);
        }

        public Address GetAddress(ECKeyPair keyPair)
        {
            return Address.FromPublicKey(keyPair.PublicKey);
        }

        private Transaction[] GetGenesisTransactions(int chainId, params Type[] contractTypes)
        {
            return contractTypes.Select(contractType => GetTransactionForDeployment(chainId, contractType)).ToArray();
        }

        private Transaction GetTransactionForDeployment(int chainId, Type contractType)
        {
            var zeroAddress = Address.BuildContractAddress(chainId, 0);

            var code = File.ReadAllBytes(contractType.Assembly.Location);
            return new Transaction
            {
                From = zeroAddress,
                To = zeroAddress,
                MethodName = nameof(ISmartContractZero.DeploySmartContract),
                Params = ByteString.CopyFrom(ParamsPacker.Pack(2, code))
            };
        }

        private Address GetContractAddress(ulong serialNumber)
        {
            return Address.BuildContractAddress(ChainHelpers.ConvertBase58ToChainId("AELF"), serialNumber);
        }

        public List<Type> GetDefaultContractTypes()
        {
            var list = new List<Type>();
            list.Add(typeof(BasicContractZero));
            list.Add(typeof(ConsensusContract));
            list.Add(typeof(TokenContract));
            list.Add(typeof(CrossChainContract));
            list.Add(typeof(AuthorizationContract));
            list.Add(typeof(ResourceContract));
            list.Add(typeof(DividendsContract));
            list.Add(typeof(FeeReceiverContract));

            return list;
        }

        public void Dispose()
        {
        }
    }
}