using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS;
using AElf.WebApp.Application.Chain.Dto;
using Google.Protobuf;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElf.WebApp.Application.Chain.Tests
{
    public sealed class ChainAppServiceTest : WebAppTestBase
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITxHub _txHub;
        private readonly OSTestHelper _osTestHelper;

        public ChainAppServiceTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _txHub = GetRequiredService<ITxHub>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
        }
        
        [Fact]
        public async Task GetBlockHeightTest()
        {
            // Get current height
            var response = await GetResponseAsStringAsync("/api/chain/chain/blockHeight");
            var currentHeight = long.Parse(response);

            var chain = await _blockchainService.GetChainAsync();
            currentHeight.ShouldBe(chain.BestChainHeight);
            
            // Mined one block
            var transaction = await _osTestHelper.GenerateTransferTransaction();
            await _osTestHelper.BroadcastTransactions(new List<Transaction> {transaction});
            await _osTestHelper.MinedOneBlock();

            // Get latest height
            response = await GetResponseAsStringAsync("/api/chain/chain/blockHeight");
            var height = long.Parse(response);
            height.ShouldBe(currentHeight + 1);
        }
        
        [Fact]
        public async Task GetChainInformationTest()
        {
            var chainId = _blockchainService.GetChainId();
            var basicContractZero = _smartContractAddressService.GetZeroSmartContractAddress();

            var response = await GetResponseAsObjectAsync<GetChainInformationOutput>("/api/chain/chain/chainInformation");

            var responseZeroContractAddress = response.GenesisContractAddress;
            var responseChainId = ChainHelpers.ConvertBase58ToChainId(response.ChainId);

            responseZeroContractAddress.ShouldBe(basicContractZero.GetFormatted());
            responseChainId.ShouldBe(chainId);
        }

        [Fact]
        public async Task Call_Success()
        {
            // Generate a transaction
            var transaction = await GenerateViewTransaction(nameof(TokenContract.GetTokenInfo), 
                new GetTokenInfoInput
                {
                    Symbol = "ELF"
                });
            
            var paramters = new Dictionary<string, string>
            {
                {"rawTransaction", transaction.ToByteArray().ToHex()}
            };

            var response = await PostResponseAsStringAsync("/api/chain/chain/call",paramters);
            response.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async Task Call_Failed()
        {
            var paramters = new Dictionary<string, string>
            {
                {"rawTransaction", "0a200a1e4604ccbdaa377fd7022b56436b99309e8b71cc5d78e909d271dbd1aeee6412200a1eaaa58b6cf58d4ef337f6dc55b701fd57d622015a3548a91a4e40892aa355180b220436957f93320c476574546f6b656e496e666f3a060a04454c46324a416246d781d80759d8ae6bb895b17203a3c9d4e89f083d7d89d9b6cbbf1c67ded52e134108fc8b3646f6549313868ce3e68a7117815cc0c2107ef1a986430a12ba002"}
            };
            var response = await PostResponseAsObjectAsync<WebAppErrorResponse>("/api/chain/chain/call",paramters,HttpStatusCode.Forbidden);
            
            response.Error.Code.ShouldBe(Error.InvalidTransaction.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.InvalidTransaction]);
        }
        
        [Fact]
        public async Task Broadcast_Transaction_Success()
        {
            // Generate a transaction
            var transaction = await _osTestHelper.GenerateTransferTransaction();
            var transactionHash = transaction.GetHash();
            
            var parameters = new Dictionary<string,string>
            {
                {"rawTransaction",transaction.ToByteArray().ToHex()}
            };
            
            var broadcastTransactionResponse =
                await PostResponseAsObjectAsync<BroadcastTransactionOutput>("/api/chain/chain/broadcastTransaction", parameters);

            broadcastTransactionResponse.TransactionId.ShouldBe(transactionHash.ToHex());

            var existTransaction = await _txHub.GetExecutableTransactionSetAsync();
            existTransaction.Transactions[0].GetHash().ShouldBe(transactionHash);
        }
        
        [Fact]
        public async Task Broadcast_Transaction_ReturnInvalidTransaction()
        {
            var fakeTransaction = "FakeTransaction";
            var parameters = new Dictionary<string,string>
            {
                {"rawTransaction",fakeTransaction}
            };
            var response =
                await PostResponseAsObjectAsync<WebAppErrorResponse>("/api/chain/chain/broadcastTransaction",
                    parameters, HttpStatusCode.Forbidden);

            response.Error.Code.ShouldBe(Error.InvalidTransaction.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.InvalidTransaction]);
        }
        
        [Fact]
        public async Task Broadcast_UnableVerify_Transaction_ReturnInvalidTransaction()
        {
            // Generate unsigned transaction
            var transaction = await _osTestHelper.GenerateTransferTransaction();
            transaction.Sigs.Clear();
            
            var parameters = new Dictionary<string,string>
            {
                {"rawTransaction",transaction.ToByteArray().ToHex()}
            };
            var response = await PostResponseAsObjectAsync<WebAppErrorResponse>(
                "/api/chain/chain/broadcastTransaction", parameters, HttpStatusCode.Forbidden);

            response.Error.Code.ShouldBe(Error.InvalidTransaction.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.InvalidTransaction]);

            var existTransaction = await _txHub.GetTransactionReceiptAsync(transaction.GetHash());
            existTransaction.ShouldBeNull();
        }
        
        [Fact]
        public async Task BroadcastTransactionsTest()
        {
            // Generate two transactions
            var transaction1 = await _osTestHelper.GenerateTransferTransaction();
            var transaction2 = await _osTestHelper.GenerateTransferTransaction();
            var transactions = new List<Transaction> {transaction1, transaction2};
            var rawTransactions = string.Join(',', transactions.Select(t => t.ToByteArray().ToHex()));

            var parameters = new Dictionary<string,string>
            {
                {"rawTransactions",rawTransactions}
            };
            var broadcastTransactionsResponse =
                await PostResponseAsObjectAsync<string[]>("/api/chain/chain/broadcastTransactions", parameters);
            var responseTransactionIds = broadcastTransactionsResponse.ToList();

            responseTransactionIds.Count.ShouldBe(2);

            var existTransaction = await _txHub.GetExecutableTransactionSetAsync();
            existTransaction.Transactions.Select(x=>x.GetHash().ToHex()).ShouldContain(responseTransactionIds[0].ToString());
            existTransaction.Transactions.Select(x=>x.GetHash().ToHex()).ShouldContain(responseTransactionIds[1].ToString());

            var getTransactionPoolStatusResponse = await GetResponseAsObjectAsync<GetTransactionPoolStatusOutput>("/api/chain/chain/transactionPoolStatus");
            getTransactionPoolStatusResponse.Queued.ShouldBe(2);
        }
        
        private Task<Transaction> GenerateViewTransaction(string method, IMessage input)
        {
            var newUserKeyPair = CryptoHelpers.GenerateKeyPair();

            var transaction = _osTestHelper.GenerateTransaction(Address.FromPublicKey(newUserKeyPair.PublicKey),
                _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name),
                method, input);
            
            var signature =
                CryptoHelpers.SignWithPrivateKey(newUserKeyPair.PrivateKey, transaction.GetHash().DumpByteArray());
            transaction.Sigs.Add(ByteString.CopyFrom(signature));

            return Task.FromResult(transaction);
        }
    }
}