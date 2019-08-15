using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Deployer;
using AElf.Contracts.TestContract.BasicFunctionWithParallel;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Miner;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS;
using AElf.OS.Node.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Volo.Abp.Threading;

namespace AElf.Parallel.Tests
{
    public class ParallelTestHelper : OSTestHelper
    {
        private IReadOnlyDictionary<string, byte[]> _codes;
        private readonly IStaticChainInformationProvider _staticChainInformationProvider;
        private readonly IAccountService _accountService;
        private readonly IMiningService _miningService;
        private readonly IBlockchainService _blockchainService;
        
        public new IReadOnlyDictionary<string, byte[]> Codes =>
            _codes ?? (_codes = ContractsDeployer.GetContractCodes<ParallelTestHelper>());
        
        public byte[] BasicFunctionWithParallelContractCode =>
            Codes.Single(kv => kv.Key.Split(",").First().Trim().EndsWith("BasicFunctionWithParallel")).Value;
        
        public Address BasicFunctionWithParallelContractAddress { get; private set; }

        public ParallelTestHelper(IOsBlockchainNodeContextService osBlockchainNodeContextService,
            IAccountService accountService,
            IMinerService minerService,
            IBlockchainService blockchainService,
            ITxHub txHub,
            ISmartContractAddressService smartContractAddressService,
            IBlockAttachService blockAttachService,
            IStaticChainInformationProvider staticChainInformationProvider,
            ITransactionResultService transactionResultService,
            IOptionsSnapshot<ChainOptions> chainOptions,IMiningService miningService) : base(osBlockchainNodeContextService, accountService,
            minerService, blockchainService, txHub, smartContractAddressService, blockAttachService,
            staticChainInformationProvider, transactionResultService, chainOptions)
        {
            _accountService = accountService;
            _staticChainInformationProvider = staticChainInformationProvider;
            _miningService = miningService;
            _blockchainService = blockchainService;
        }

        public override Block GenerateBlock(Hash preBlockHash, long preBlockHeight, IEnumerable<Transaction> transactions = null)
        {
            var block = new Block
            {
                Header = new BlockHeader
                {
                    ChainId = _staticChainInformationProvider.ChainId,
                    Height = preBlockHeight + 1,
                    PreviousBlockHash = preBlockHash,
                    Time = TimestampHelper.GetUtcNow(),
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
            }

            return block;
        }

        public async Task DeployBasicFunctionWithParallelContract()
        {
            BasicFunctionWithParallelContractAddress = await DeployContract<BasicFunctionWithParallelContract>();
        }
        
        public List<Transaction> GenerateBasicFunctionWithParallelTransactions(int groupCount,int transactionCount)
        {
            var transactions = new List<Transaction>();
            
            for (var i = 0; i < groupCount; i++)
            {
                var keyPair = CryptoHelper.GenerateKeyPair();
                var from = Address.FromPublicKey(keyPair.PublicKey);
                var count = transactionCount / groupCount;
                for (var j = 0; j < count; j++)
                {
                    var address = Address.FromPublicKey(CryptoHelper.GenerateKeyPair().PublicKey);
                    var transaction = GenerateTransaction(from,
                        BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                            .QueryTwoUserWinMoney),
                        new QueryTwoUserWinMoneyInput
                            {First = from, Second = address});
                    var signature =
                        CryptoHelper.SignWithPrivateKey(keyPair.PrivateKey, transaction.GetHash().ToByteArray());
                    transaction.Signature = ByteString.CopyFrom(signature); 

                    transactions.Add(transaction);
                }
            }

            return transactions;
        }

        public Transaction CreateTransaction(ECKeyPair from, Address to, string methodName,
            ByteString parameters, long blockHeight, Hash blockHash)
        {
            var transaction = new Transaction
            {
                From = Address.FromPublicKey(from.PublicKey),
                To = to,
                MethodName = methodName,
                Params = parameters,
                RefBlockNumber = blockHeight,
                RefBlockPrefix = ByteString.CopyFrom(blockHash.Value.Take(4).ToArray()),
            };
            var signature = CryptoHelper.SignWithPrivateKey(from.PrivateKey,
                transaction.GetHash().Value.ToByteArray());
            transaction.Signature = ByteString.CopyFrom(signature);
            return transaction;
        }
        
        public async Task<Block> ExecuteAsync(Transaction transaction,long previousBlockHeight,Hash previousBlockHash)
        {
            var transactionList = new List<Transaction>();
            if(transaction!=null) transactionList.Add(transaction);
            var block = await _miningService.MineAsync(
                new RequestMiningDto
                {
                    PreviousBlockHash = previousBlockHash, PreviousBlockHeight = previousBlockHeight,
                    BlockExecutionTime = TimestampHelper.DurationFromMilliseconds(int.MaxValue)
                },
                transactionList,
                DateTime.UtcNow.ToTimestamp());

            if(transaction != null)
                await _blockchainService.AddTransactionsAsync(new List<Transaction> {transaction});
            await _blockchainService.AddBlockAsync(block);
            return block;
        }
    }
}