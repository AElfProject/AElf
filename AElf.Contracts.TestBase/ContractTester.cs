using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
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
using AElf.Kernel.Services;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Node.Application;
using AElf.Types.CSharp;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace AElf.Contracts.TestBase
{
    public class ContractTester : ITransientDependency
    {
        private readonly int _chainId;

        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionExecutingService _transactionExecutingService;
        private readonly IBlockchainNodeContextService _blockchainNodeContextService;
        private readonly IBlockGenerationService _blockGenerationService;
        private readonly ISystemTransactionGenerationService _systemTransactionGenerationService;
        private readonly IBlockExecutingService _blockExecutingService;
        private readonly IConsensusService _consensusService;
        private readonly IChainManager _chainManager;

        public ECKeyPair CallOwner { get; set; }

        public ContractTester(int chainId)
        {
            _chainId = chainId;

            var application =
                AbpApplicationFactory.Create<ContractTestAElfModule>(options =>
                {
                    options.UseAutofac();
                    options.Services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
                    options.Services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());
                });
            application.Initialize();

            _blockchainService = application.ServiceProvider.GetService<IBlockchainService>();
            _transactionExecutingService = application.ServiceProvider.GetService<ITransactionExecutingService>();
            _blockchainNodeContextService = application.ServiceProvider.GetService<IBlockchainNodeContextService>();
            _blockGenerationService = application.ServiceProvider.GetService<IBlockGenerationService>();
            _systemTransactionGenerationService =
                application.ServiceProvider.GetService<ISystemTransactionGenerationService>();
            _blockExecutingService = application.ServiceProvider.GetService<IBlockExecutingService>();
            _consensusService = application.ServiceProvider.GetService<IConsensusService>();
            _chainManager = application.ServiceProvider.GetService<IChainManager>();

            CallOwner = CryptoHelpers.GenerateKeyPair();
        }

        public void SetCallOwner(ECKeyPair caller)
        {
            CallOwner = caller;
        }

        public Address GetCallOwnerAddress()
        {
            return GetAddress(CallOwner);
        }

        public async Task<List<Address>> InitialChainAsync(params Type[] contractTypes)
        {
            var transactions = GetGenesisTransactions(_chainId, contractTypes);
            var dto = new OsBlockchainNodeContextStartDto
            {
                BlockchainNodeContextStartDto = new BlockchainNodeContextStartDto
                {
                    ChainId = _chainId,
                    Transactions = transactions
                }
            };

            await _blockchainNodeContextService.StartAsync(dto.BlockchainNodeContextStartDto);

            var addresses = new List<Address>();
            for (var i = 0UL; i < (ulong) contractTypes.Length; i++)
            {
                addresses.Add(GetContractAddress(i));
            }

            return addresses;
        }

        public Transaction GenerateTransaction(Address contractAddress, string methodName,
            params object[] objects)
        {
            var tx = new Transaction
            {
                From = GetAddress(CallOwner),
                To = contractAddress,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(objects))
            };

            var signature = CryptoHelpers.SignWithPrivateKey(CallOwner.PrivateKey, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature));

            return tx;
        }

        public async Task<ByteString> ExecuteContractAsync(Address contractAddress, string methodName,
            params object[] objects)
        {
            var tx = new Transaction
            {
                From = GetAddress(CallOwner),
                To = contractAddress,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(objects))
            };

            var signature = CryptoHelpers.SignWithPrivateKey(CallOwner.PrivateKey, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature));

            var preBlock = await _blockchainService.GetBestChainLastBlock(_chainId);
            var executionReturnSets = await _transactionExecutingService.ExecuteAsync(new ChainContext
                {
                    ChainId = _chainId,
                    BlockHash = preBlock.GetHash(),
                    BlockHeight = preBlock.Height
                },
                new List<Transaction> {tx},
                DateTime.UtcNow, new CancellationToken());

            return executionReturnSets.Any() ? executionReturnSets.Last().ReturnValue : null;
        }

        public async Task<IBlock> MineABlockAsync(List<Transaction> txs)
        {
            var preBlock = await _blockchainService.GetBestChainLastBlock(_chainId);
            var minerService = BuildMinerService(txs);
            return await minerService.MineAsync(_chainId, preBlock.GetHash(), preBlock.Height,
                DateTime.UtcNow.AddMilliseconds(4000));
        }

        public async Task<Chain> GetChainAsync()
        {
            return await _blockchainService.GetChainAsync(_chainId);
        }

        public async Task AddABlockAsync(Block block)
        {
            await _blockchainService.AddBlockAsync(_chainId, block);
            var chain = await _blockchainService.GetChainAsync(_chainId);
            await _blockchainService.AttachBlockToChainAsync(chain, block);
        }

        public async Task SetIrreversibleBlock(Hash libHash)
        {
            var chain = await _blockchainService.GetChainAsync(_chainId);
            await _chainManager.SetIrreversibleBlockAsync(chain, libHash);
        }

        public async Task SetIrreversibleBlock(ulong libHeight)
        {
            var chain = await _blockchainService.GetChainAsync(_chainId);
            var libHash = (await _blockchainService.GetBlockByHeightAsync(_chainId, libHeight)).GetHash();
            chain.LastIrreversibleBlockHash = libHash;
            chain.LastIrreversibleBlockHeight = libHeight;
            await _chainManager.SetIrreversibleBlockAsync(chain, libHash);
        }

        private MinerService BuildMinerService(List<Transaction> txs)
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

            var mockTxHub = new Mock<ITxHub>();
            mockTxHub.Setup(h => h.GetReceiptsOfExecutablesAsync()).ReturnsAsync(trs);

            var mockAccountService = new Mock<IAccountService>();
            mockAccountService.Setup(s => s.GetPublicKeyAsync())
                .ReturnsAsync(CryptoHelpers.GenerateKeyPair().PublicKey);
            return new MinerService(mockTxHub.Object, mockAccountService.Object, _blockGenerationService,
                _systemTransactionGenerationService, _blockchainService, _blockExecutingService, _consensusService);
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
    }
}