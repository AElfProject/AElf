using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Deployer;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestContract.BasicFunctionWithParallel;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.Token;
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
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        
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
            IOptionsSnapshot<ChainOptions> chainOptions,
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService
            ) : base(osBlockchainNodeContextService, accountService,
            minerService, blockchainService, txHub, smartContractAddressService, blockAttachService,
            staticChainInformationProvider, transactionResultService, chainOptions)
        {
            _accountService = accountService;
            _staticChainInformationProvider = staticChainInformationProvider;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _smartContractAddressService = smartContractAddressService;

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

        public async Task<ByteString> ExecuteReadOnlyAsync(Transaction transaction, Hash blockHash,long blockHeight)
        {
            var transactionTrace = await _transactionReadOnlyExecutionService.ExecuteAsync(new ChainContext
                {
                    BlockHash = blockHash,
                    BlockHeight = blockHeight
                },
                transaction,
                DateTime.UtcNow.ToTimestamp());

            return transactionTrace.ReturnValue;
        }

        public async Task<long> QueryBalanceAsync(Address address,string symbol,Hash blockHash,long blockHeight)
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var transaction = GenerateTransaction(accountAddress,
                _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name),
                nameof(TokenContractContainer.TokenContractStub.GetBalance),
                new GetBalanceInput {Owner = address, Symbol = symbol});
            var returnValue = await ExecuteReadOnlyAsync(transaction, blockHash, blockHeight);
            return GetBalanceOutput.Parser.ParseFrom(returnValue).Balance;
        }
    }
}