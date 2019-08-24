using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.Token;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS;
using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using Enum = System.Enum;

namespace AElf.WebApp.Application.Chain.Tests
{
    public class TransactionToHubTests : WebAppTestBase
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITxHub _txHub;
        private readonly IBlockchainStateMergingService _blockchainStateMergingService;
        private readonly IBlockchainStateManager _blockchainStateManager;
        private readonly OSTestHelper _osTestHelper;
        private readonly IAccountService _accountService;
        private readonly ITaskQueueManager _taskQueueManager;
        
        public TransactionToHubTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _txHub = GetRequiredService<ITxHub>();
            _blockchainStateMergingService = GetRequiredService<IBlockchainStateMergingService>();
            _blockchainStateManager = GetRequiredService<IBlockchainStateManager>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _accountService = GetRequiredService<IAccountService>();
            _taskQueueManager = GetRequiredService<ITaskQueueManager>();
        }

        [Fact]
        public async Task SentTransaction_Token_Check_Test()
        {
            //user without token - NotExisted
            {
                var keyPair = CryptoHelper.GenerateKeyPair();
                var tokenAddress = GetTokenContractAddress();

                //approve transaction
                var transaction = await GenerateTransaction(keyPair, tokenAddress, "Approve", new ApproveInput
                {
                    Amount = 100,
                    Spender = Address.FromPublicKey(CryptoHelper.GenerateKeyPair().PublicKey),
                    Symbol = "Check user without token whether can sent transaction"
                });

                var transactionId = await SendTransactionAsync(transaction);
                var transactionResult = await QueryTransactionResultAsync(transactionId);
                Enum.TryParse<TransactionResultStatus>(transactionResult.Status, true, out var status);
                status.ShouldBe(TransactionResultStatus.NotExisted);
            }
            
            //bp user with token - Mined
            {
                var transaction = await _osTestHelper.GenerateTransferTransaction();
                var transactionId = await SendTransactionAsync(transaction);
                var transactionResult = await QueryTransactionResultAsync(transactionId);
                Enum.TryParse<TransactionResultStatus>(transactionResult.Status, true, out var status);
                status.ShouldBe(TransactionResultStatus.Mined);
            }
        }

        [Fact]
        public async Task SentTransaction_ContractAddress_Check_Test()
        {
            //prepare token
            var (transactions, keyPairs) = await _osTestHelper.PrepareTokenForParallel(2, 100);
            await SendTransactionAsync(transactions[0]);

            var fakeContract = Address.FromPublicKey(keyPairs[1].PublicKey);
            var transaction = await GenerateTransaction(keyPairs[0], fakeContract, "ContractTest", new Empty());
            
            var transactionId = await SendTransactionAsync(transaction);
            var transactionResult = await QueryTransactionResultAsync(transactionId);
            Enum.TryParse<TransactionResultStatus>(transactionResult.Status, true, out var status);
            status.ShouldBe(TransactionResultStatus.NotExisted); 
        }

        [Fact]
        public async Task SentTransaction_SystemContract_Check_Test()
        {
            //prepare token
            var (transactions, keyPairs) = await _osTestHelper.PrepareTokenForParallel(1, 100);
            await SendTransactionAsync(transactions[0]);

            //send consensus transaction
            var consensusContract = GetConsensusContractAddress();
            var transaction = await GenerateTransaction(keyPairs[0], consensusContract, "FirstRound", new Round());

            var transactionId = await SendTransactionAsync(transaction);
            var transactionResult = await QueryTransactionResultAsync(transactionId);
            Enum.TryParse<TransactionResultStatus>(transactionResult.Status, true, out var status);
            status.ShouldBe(TransactionResultStatus.NotExisted);
        }

        private async Task<string> SendTransactionAsync(Transaction transaction)
        {
            await _osTestHelper.BroadcastTransactions(new List<Transaction> {transaction});
            await _osTestHelper.MinedOneBlock();

            return transaction.GetHash().ToHex();
        }

        private async Task<TransactionResultDto> QueryTransactionResultAsync(string transactionId)
        {
            return await GetResponseAsObjectAsync<TransactionResultDto>(
                $"/api/blockChain/transactionResult?transactionId={transactionId}");
        }
        
        private async Task<Transaction> GenerateTransaction(ECKeyPair keyPair, Address contract, string method,
            IMessage input)
        {
            var transaction = new Transaction
            {
                From = Address.FromPublicKey(keyPair.PublicKey),
                To = contract,
                MethodName = method,
                Params = input.ToByteString()
            };
            return await AddReferenceAndSignAsync(keyPair, transaction);
        }

        private async Task<Transaction> AddReferenceAndSignAsync(ECKeyPair keyPair, Transaction transaction)
        {
            var chain = await _blockchainService.GetChainAsync();
            transaction.RefBlockNumber = chain.BestChainHeight;
            transaction.RefBlockPrefix = ByteString.CopyFrom(chain.BestChainHash.Value.Take(4).ToArray());
            
            transaction.Signature =
                ByteString.CopyFrom(CryptoHelper.SignWithPrivateKey(keyPair.PrivateKey,
                    transaction.GetHash().ToByteArray()));

            return transaction;
        }

        private Address GetTokenContractAddress()
        {
            return _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);
        }

        private Address GetConsensusContractAddress()
        {
            return _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider
                .Name);
        }
    }
}