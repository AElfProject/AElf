using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Genesis;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Node.Application;
using AElf.Kernel.Services;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS;
using AElf.OS.Account;
using AElf.OS.Node.Application;
using AElf.Types.CSharp;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace AElf.Contracts.Consensus.Tests
{
    public class ContractTestHelper : ITransientDependency
    {
        private readonly int _chainId;

        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionExecutingService _transactionExecutingService;
        private readonly IBlockchainNodeContextService _blockchainNodeContextService;
        private readonly IBlockGenerationService _blockGenerationService;
        private readonly ISystemTransactionGenerationService _systemTransactionGenerationService;
        private readonly IBlockExecutingService _blockExecutingService;
        private readonly IConsensusService _consensusService;

        public ContractTestHelper(int chainId)
        {
            _chainId = chainId;

            var application =
                AbpApplicationFactory.Create<ConsensusContractTestAElfModule>(options => { options.UseAutofac(); });
            application.Initialize();

            _blockchainService = application.ServiceProvider.GetService<IBlockchainService>();
            _transactionExecutingService = application.ServiceProvider.GetService<ITransactionExecutingService>();
            _blockchainNodeContextService = application.ServiceProvider.GetService<IBlockchainNodeContextService>();
            _blockGenerationService = application.ServiceProvider.GetService<IBlockGenerationService>();
            _systemTransactionGenerationService = application.ServiceProvider.GetService<ISystemTransactionGenerationService>();
            _blockExecutingService = application.ServiceProvider.GetService<IBlockExecutingService>();
            _consensusService = application.ServiceProvider.GetService<IConsensusService>();
        }

        public async Task InitialChainAsync()
        {
            var transactions = GetGenesisTransactions(_chainId);
            var dto = new OsBlockchainNodeContextStartDto
            {
                BlockchainNodeContextStartDto = new BlockchainNodeContextStartDto()
                {
                    ChainId = _chainId,
                    Transactions = transactions
                }
            };

            await _blockchainNodeContextService.StartAsync(dto.BlockchainNodeContextStartDto);
        }

        public Transaction GenerateTransaction(Address contractAddress, string methodName,
            ECKeyPair callerKeyPair, params object[] objects)
        {
            var tx = new Transaction
            {
                From = GetAddress(callerKeyPair),
                To = contractAddress,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(objects))
            };

            var signature = CryptoHelpers.SignWithPrivateKey(callerKeyPair.PrivateKey, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature));

            return tx;
        }

        public async Task<ByteString> ExecuteContractAsync(Address contractAddress, string methodName,
            ECKeyPair callerKeyPair, params object[] objects)
        {
            var tx = new Transaction
            {
                From = GetAddress(callerKeyPair),
                To = contractAddress,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(objects))
            };

            var signature = CryptoHelpers.SignWithPrivateKey(callerKeyPair.PrivateKey, tx.GetHash().DumpByteArray());
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

        public async Task MineABlockAsync(List<Transaction> txs)
        {
            var preBlock = await _blockchainService.GetBestChainLastBlock(_chainId);
            var minerService = BuildMinerService(txs);
            await minerService.MineAsync(_chainId, preBlock.GetHash(), preBlock.Height,
                DateTime.UtcNow.AddMilliseconds(4000));
        }

        public async Task<Chain> GetChainAsync()
        {
            return await _blockchainService.GetChainAsync(_chainId);
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
        
        private Address GetAddress(ECKeyPair keyPair)
        {
            return Address.FromPublicKey(keyPair.PublicKey);
        }

        private Transaction[] GetGenesisTransactions(int chainId)
        {
            var transactions = new List<Transaction>();
            transactions.Add(GetTransactionForDeployment(chainId, typeof(BasicContractZero)));
            transactions.Add(GetTransactionForDeployment(chainId, typeof(DPoS.Contract)));
            return transactions.ToArray();
        }

        private Transaction GetTransactionForDeployment(int chainId, Type contractType)
        {
            var zeroAddress = Address.BuildContractAddress(chainId, 0);
            var code = File.ReadAllBytes(contractType.Assembly.Location);
            return new Transaction()
            {
                From = zeroAddress,
                To = zeroAddress,
                MethodName = nameof(ISmartContractZero.DeploySmartContract),
                Params = ByteString.CopyFrom(ParamsPacker.Pack(2, code))
            };
        }
    }
}