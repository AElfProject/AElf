using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Runtime.CSharp;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElf.OS.Rpc.ChainController.Tests
{
    public class ChainControllerRpcServiceServerTest : RpcTestBase
    {
        public ILogger<ChainControllerRpcServiceServerTest> Logger { get; set; }
        private readonly IBlockchainService _blockchainService;
        private readonly ITxHub _txHub;
        private readonly IAccountService _accountService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly OSTestHelper _osTestHelper;

        private const int DefaultCategory = 3;

        public ChainControllerRpcServiceServerTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            Logger = GetService<ILogger<ChainControllerRpcServiceServerTest>>() ??
                     NullLogger<ChainControllerRpcServiceServerTest>.Instance;

            _blockchainService = GetRequiredService<IBlockchainService>();
            _txHub = GetRequiredService<ITxHub>();
            _accountService = GetRequiredService<IAccountService>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
        }

        #region Chain cases

        [Fact]
        public async Task Get_Commands_Success()
        {
            //Get commands
            var response = await JsonCallAsJObject("/chain", "GetCommands");
            var commands = response["result"].ToList();
            commands.Count.ShouldBeGreaterThan(1);
        }

        [Fact]
        public async Task Get_BlockHeight_Success()
        {
            // Get current height
            var response = await JsonCallAsJObject("/chain", "GetBlockHeight");
            var currentHeight = (int) response["result"];

            // Mined one block
            var chain = await _blockchainService.GetChainAsync();
            var transaction = await _osTestHelper.GenerateTransferTransaction();
            await _osTestHelper.BroadcastTransactions(new List<Transaction> {transaction});
            await _osTestHelper.MinedOneBlock();

            // Get latest height
            response = await JsonCallAsJObject("/chain", "GetBlockHeight");
            var height = (int) response["result"];
            height.ShouldBe(currentHeight + 1);
        }

        [Fact]
        public async Task Get_Chain_Information_Success()
        {
            var chainId = _blockchainService.GetChainId();
            var basicContractZero = _smartContractAddressService.GetZeroSmartContractAddress();

            var response = await JsonCallAsJObject("/chain", "GetChainInformation");

            var responseZeroContractAddress = response["result"]["GenesisContractAddress"].ToString();
            var responseChainId = ChainHelper.ConvertBase58ToChainId(response["result"]["ChainId"].ToString());

            responseZeroContractAddress.ShouldBe(basicContractZero.GetFormatted());
            responseChainId.ShouldBe(chainId);
        }

        [Fact]
        public async Task Broadcast_Transaction_Success()
        {
            // Generate a transaction
            var transaction = await _osTestHelper.GenerateTransferTransaction();
            var transactionId = transaction.GetHash();

            var response = await JsonCallAsJObject("/chain", "BroadcastTransaction",
                new {rawTransaction = transaction.ToByteArray().ToHex()});
            var responseTransactionId = response["result"]["TransactionId"].ToString();

            responseTransactionId.ShouldBe(transactionId.ToHex());

            var existTransaction = await _txHub.GetExecutableTransactionSetAsync();
            existTransaction.Transactions[0].GetHash().ShouldBe(transactionId);
        }

        [Fact]
        public async Task Call_ReadOnly_Success()
        {
            // Generate a transaction
            var chain = await _blockchainService.GetChainAsync();
            var basicContractZero = _smartContractAddressService.GetZeroSmartContractAddress();
            var transaction = new Transaction
            {
                From = SampleAddress.AddressList[0],
                To = basicContractZero,
                MethodName = "GetContractInfo",
                Params = basicContractZero.ToByteString()
            };

            var response = await JsonCallAsJObject("/chain", "Call",
                new {rawTransaction = transaction.ToByteArray().ToHex()});
            var resultString = response["result"].ToString();
            resultString.ShouldNotBeNullOrEmpty();

            // The following is always true
//            var bs = ByteArrayHelpers.FromHexString(resultString);
//            var contractInfo = ContractInfo.Parser.ParseFrom(bs);
//            contractInfo.ShouldNotBeNull();
        }

        [Fact]
        public async Task Broadcast_Transaction_ReturnInvalidTransaction()
        {
            var fakeTransaction = "FakeTransaction";
            var response = await JsonCallAsJObject("/chain", "BroadcastTransaction",
                new {rawTransaction = fakeTransaction});
            var responseCode = (long) response["error"]["code"];
            var responseMessage = response["error"]["message"].ToString();

            responseCode.ShouldBe(Error.InvalidTransaction);
            responseMessage.ShouldBe(Error.Message[Error.InvalidTransaction]);
        }

        [Fact]
        public async Task Broadcast_UnableVerify_Transaction_ReturnInvalidTransaction()
        {
            // Generate unsigned transaction
            var transaction = await _osTestHelper.GenerateTransferTransaction();
            transaction.Signature = ByteString.CopyFrom(new byte[0]);

            var response = await JsonCallAsJObject("/chain", "BroadcastTransaction",
                new {rawTransaction = transaction.ToByteArray().ToHex()});
            var responseCode = (long) response["error"]["code"];
            var responseMessage = response["error"]["message"].ToString();

            responseCode.ShouldBe(Error.InvalidTransaction);
            responseMessage.ShouldBe(Error.Message[Error.InvalidTransaction]);

            var existTransaction = await _txHub.GetTransactionReceiptAsync(transaction.GetHash());
            existTransaction.ShouldBeNull();
        }

        [Fact]
        public async Task Broadcast_Transactions_Success()
        {
            // Generate two transactions
            var transaction1 = await _osTestHelper.GenerateTransferTransaction();
            var transaction2 = await _osTestHelper.GenerateTransferTransaction();
            var transactions = new List<Transaction> {transaction1, transaction2};
            var rawTransactions = string.Join(',', transactions.Select(t => t.ToByteArray().ToHex()));

            var response = await JsonCallAsJObject("/chain", "BroadcastTransactions",
                new {rawTransactions = rawTransactions});
            var responseTransactionIds = response["result"].ToList();

            responseTransactionIds.Count.ShouldBe(2);

            var existTransaction = await _txHub.GetExecutableTransactionSetAsync();
            existTransaction.Transactions.Select(x=>x.GetHash().ToHex()).ShouldContain(responseTransactionIds[0].ToString());
            existTransaction.Transactions.Select(x=>x.GetHash().ToHex()).ShouldContain(responseTransactionIds[1].ToString());

            response = await JsonCallAsJObject("/chain", "GetTransactionPoolStatus");
            response["result"]["Queued"].ShouldBe(2);
        }

        [Fact]
        public async Task Get_TransactionResult_Success()
        {
            // Generate a transaction and broadcast
            var transaction = await _osTestHelper.GenerateTransferTransaction();
            var transactionHex = transaction.GetHash().ToHex();
            await _osTestHelper.BroadcastTransactions(new List<Transaction> {transaction});

            // Before mined
            var response = await JsonCallAsJObject("/chain", "GetTransactionResult",
                new {transactionId = transactionHex});
            var responseTransactionId = response["result"]["TransactionId"].ToString();
            var responseStatus = response["result"]["Status"].ToString();

            responseTransactionId.ShouldBe(transactionHex);
            responseStatus.ShouldBe(TransactionResultStatus.Pending.ToString());

            await _osTestHelper.MinedOneBlock();

            // After mined
            response = await JsonCallAsJObject("/chain", "GetTransactionResult",
                new {transactionId = transactionHex});
            responseTransactionId = response["result"]["TransactionId"].ToString();
            responseStatus = response["result"]["Status"].ToString();

            responseTransactionId.ShouldBe(transactionHex);
            responseStatus.ShouldBe(TransactionResultStatus.Mined.ToString());
        }

        [Fact]
        public async Task Get_Failed_TransactionResult_Success()
        {
            // Generate a transaction and broadcast
            var chain = await _blockchainService.GetChainAsync();
            var transactionList = GenerateTwoInitializeTransaction();
            await _osTestHelper.BroadcastTransactions(transactionList);

            var block = await _osTestHelper.MinedOneBlock();

            // After executed
            var transactionHex = transactionList[1].GetHash().ToHex();
            var response = await JsonCallAsJObject("/chain", "GetTransactionResult",
                new {transactionId = transactionHex});
            var responseTransactionId = response["result"]["TransactionId"].ToString();
            var responseStatus = response["result"]["Status"].ToString();
            var responseErrorMessage = response["result"]["Error"].ToString();

            responseTransactionId.ShouldBe(transactionHex);
            responseStatus.ShouldBe(TransactionResultStatus.Failed.ToString());
            responseErrorMessage.Contains("Token already exists.").ShouldBeTrue();
        }
        
        [Fact]
        public async Task Get_NotExisted_TransactionResult()
        {
            // Generate a transaction
            var transaction = await _osTestHelper.GenerateTransferTransaction();
            var transactionHex = transaction.GetHash().ToHex();

            var response = await JsonCallAsJObject("/chain", "GetTransactionResult",
                new {transactionId = transactionHex});
            var responseTransactionId = response["result"]["TransactionId"].ToString();
            var responseStatus = response["result"]["Status"].ToString();

            responseTransactionId.ShouldBe(transactionHex);
            responseStatus.ShouldBe(TransactionResultStatus.NotExisted.ToString());
        }

        [Fact]
        public async Task Get_TransactionResult_ReturnInvalidTransactionId()
        {
            var fakeTransactionId = "FakeTransactionId";
            var response = await JsonCallAsJObject("/chain", "GetTransactionResult",
                new {transactionId = fakeTransactionId});
            var responseCode = (long) response["error"]["code"];
            var responseMessage = response["error"]["message"].ToString();

            responseCode.ShouldBe(Error.InvalidTransactionId);
            responseMessage.ShouldBe(Error.Message[Error.InvalidTransactionId]);
        }

        [Fact]
        public async Task Get_TransactionsResult_Success()
        {
            // Generate 20 transactions and mined
            var transactions = new List<Transaction>();
            for (int i = 0; i < 20; i++)
            {
                transactions.Add(await _osTestHelper.GenerateTransferTransaction());
            }

            await _osTestHelper.BroadcastTransactions(transactions);
            var block = await _osTestHelper.MinedOneBlock();

            var response = await JsonCallAsJObject("/chain", "GetTransactionsResult",
                new {blockHash = block.GetHash().ToHex(), offset = 0, limit = 15});

            var responseTransactionResults = response["result"].ToList();
            responseTransactionResults.Count.ShouldBe(15);

            response = await JsonCallAsJObject("/chain", "GetTransactionsResult",
                new {blockHash = block.GetHash().ToHex(), offset = 15, limit = 15});

            responseTransactionResults = response["result"].ToList();
            responseTransactionResults.Count.ShouldBe(5);
        }

        [Fact]
        public async Task Get_NotExisted_TransactionsResult()
        {
            var block = _osTestHelper.GenerateBlock(Hash.Empty, 10);
            var blockHash = block.GetHash().ToHex();
            var response = await JsonCallAsJObject("/chain", "GetTransactionsResult",
                new {blockHash, offset = 0, num = 10});

            var returnCode = (long) response["error"]["code"];
            returnCode.ShouldBe(Error.NotFound);

            var message = response["error"]["message"].ToString();
            message.ShouldBe(Error.Message[Error.NotFound]);
        }

        [Fact]
        public async Task Get_TransactionsResult_With_InvalidParameter()
        {
            var block = _osTestHelper.GenerateBlock(Hash.Empty, 10);
            var blockHash = block.GetHash().ToHex();
            
            var response1 = await JsonCallAsJObject("/chain", "GetTransactionsResult",
                new {blockHash, offset = -3, num = 10});
            response1["error"]["code"].ShouldNotBeNull();
            response1["error"]["message"].ToString().Contains("Offset must greater than or equal to 0").ShouldBeTrue();
            
            var response2 = await JsonCallAsJObject("/chain", "GetTransactionsResult",
                new {blockHash, offset = 0, num = -5});
            response2["error"]["code"].ShouldNotBeNull();
            response2["error"]["message"].ToString().Contains("Not found").ShouldBeTrue();
            
            var response3 = await JsonCallAsJObject("/chain", "GetTransactionsResult",
                new {blockHash, offset = 0, num = 120});
            response3["error"]["code"].ShouldNotBeNull();
            response3["error"]["message"].ToString().Contains("Not found").ShouldBeTrue();
        }

        [Fact]
        public async Task Get_BlockInfo_Success()
        {
            var chain = await _blockchainService.GetChainAsync();
            var transactions = new List<Transaction>();
            for (int i = 0; i < 3; i++)
            {
                transactions.Add(await _osTestHelper.GenerateTransferTransaction());
            }

            await _osTestHelper.BroadcastTransactions(transactions);
            var block = await _osTestHelper.MinedOneBlock();

            var response = await JsonCallAsJObject("/chain", "GetBlockInfo",
                new {blockHeight = 12, includeTransactions = true});
            var responseResult = response["result"];

            responseResult["BlockHash"].ToString().ShouldBe(block.GetHash().ToHex());
            responseResult["Header"]["PreviousBlockHash"].ToString()
                .ShouldBe(block.Header.PreviousBlockHash.ToHex());
            responseResult["Header"]["MerkleTreeRootOfTransactions"].ToString().ShouldBe(
                block.Header.MerkleTreeRootOfTransactions.ToHex
                    ());
            responseResult["Header"]["MerkleTreeRootOfWorldState"].ToString()
                .ShouldBe(block.Header.MerkleTreeRootOfWorldState.ToHex());
            ((long) responseResult["Header"]["Height"]).ShouldBe(block.Height);
            Convert.ToDateTime(responseResult["Header"]["Time"]).ShouldBe(block.Header.Time.ToDateTime());
            responseResult["Header"]["ChainId"].ToString().ShouldBe(ChainHelper.ConvertChainIdToBase58(chain.Id));
            responseResult["Header"]["Bloom"].ToString().ShouldBe(block.Header.Bloom.ToByteArray().ToHex());
            ((int) responseResult["Body"]["TransactionsCount"]).ShouldBe(3);

            var responseTransactions = responseResult["Body"]["Transactions"].ToList();
            responseTransactions.Count.ShouldBe(3);
        }

        [Fact]
        public async Task Get_BlockInfo_ReturnNotFound()
        {
            var response = await JsonCallAsJObject("/chain", "GetBlockInfo",
                new {blockHeight = 100});
            var responseCode = (long) response["error"]["code"];
            var responseMessage = response["error"]["message"].ToString();

            responseCode.ShouldBe(Error.NotFound);
            responseMessage.ShouldBe(Error.Message[Error.NotFound]);
        }

        [Fact]
        public async Task Get_Chain_Status_Success()
        {
            var response = await JsonCallAsJObject("/chain", "GetChainStatus");
            response["result"]["Branches"].ShouldNotBeNull();
            Convert.ToInt32(response["result"]["BestChainHeight"]).ShouldBe(11);
        }

        [Fact]
        public async Task Get_Block_State_Success()
        {
            var transactions = new List<Transaction>();
            for (int i = 0; i < 3; i++)
            {
                transactions.Add(await _osTestHelper.GenerateTransferTransaction());
            }

            await _osTestHelper.BroadcastTransactions(transactions);
            await _osTestHelper.MinedOneBlock();

            var response = await JsonCallAsJObject("/chain", "GetBlockInfo",
                new {blockHeight = 12, includeTransactions = true});
            var responseResult = response["result"];
            var blockHash = responseResult["BlockHash"].ToString();

            var response1 = await JsonCallAsJObject("/chain", "GetBlockState",
                new {blockHash = blockHash});
            response1["result"].ShouldNotBeNull();
            response1["result"]["BlockHash"].ToString().ShouldBe(blockHash);
            response1["result"]["BlockHeight"].To<int>().ShouldBe(12);
            response1["result"]["Changes"].ShouldNotBeNull();
        }

        [Fact]
        public async Task Get_Block_State_FewBlocks_Later()
        {
            var transactions = new List<Transaction>();
            for (int i = 0; i < 3; i++)
            {
                transactions.Add(await _osTestHelper.GenerateTransferTransaction());
            }

            await _osTestHelper.BroadcastTransactions(transactions);
            await _osTestHelper.MinedOneBlock();

            var response = await JsonCallAsJObject("/chain", "GetBlockInfo",
                new {blockHeight = 12, includeTransactions = true});
            var responseResult = response["result"];
            var blockHash = responseResult["BlockHash"].ToString();

            //Continue generate block 
            for (int i = 0; i < 10; i++)
            {
                await _osTestHelper.MinedOneBlock();
            }

            //Check Block State
            var response1 = await JsonCallAsJObject("/chain", "GetBlockState",
                new {blockHash = blockHash});
            response1["result"].ShouldNotBeNull();
            response1["result"]["BlockHash"].ToString().ShouldBe(blockHash);
            response1["result"]["BlockHeight"].To<int>().ShouldBe(12);
            response1["result"]["Changes"].ShouldNotBeNull();
        }

        [Fact]
        public async Task Query_NonExist_Api_Failed()
        {
            var response = await JsonCallAsJObject("/chain", "TestMethod",
                new {Test = "testParameter"});
            response.ShouldNotBeNull();
            response["error"]["code"].To<int>().ShouldBe(-32601);
            response["error"]["message"].ToString().ShouldBe("The specified method does not exist or is not available");
        }

        [Fact]
        public void Transaction_To_JObject()
        {
            var transaction = _osTestHelper.GenerateTransaction(SampleAddress.AddressList[0], SampleAddress.AddressList[1],
                nameof(TokenContractContainer.TokenContractStub.Transfer), new TransferInput
                {
                    Symbol = "ELF",
                    Amount = 1000L,
                    To = SampleAddress.AddressList[0]
                });
            var transactionObj = transaction.GetTransactionInfo();
            transactionObj.ShouldNotBeNull();
            transactionObj["Transaction"]["Method"].ToString().ShouldBe(nameof(TokenContractContainer.TokenContractStub.Transfer));
        }

        [Fact]
        public async Task Get_FileDescriptorSet_Success()
        {
            // Generate a transaction and broadcast
            var transaction = await _osTestHelper.GenerateTransferTransaction();
            await _osTestHelper.BroadcastTransactions(new List<Transaction> {transaction});
            
            await _osTestHelper.MinedOneBlock();
            
            //Result empty
            var response = await JsonCallAsJObject("/chain", "GetFileDescriptorSet",
                new {address = transaction.To.GetFormatted()});
            response["result"].ToString().ShouldNotBeEmpty();

            var set = FileDescriptorSet.Parser.ParseFrom(ByteString.FromBase64(response["result"].ToString()));
            set.ShouldNotBeNull();
            set.File.Count.ShouldBeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task Get_FileDescriptorSet_Failed()
        {
            var addressInfo = SampleAddress.AddressList[0].GetFormatted();
            var response = await JsonCallAsJObject("/chain", "GetFileDescriptorSet",
                new {address = addressInfo});
            response["error"]["code"].To<long>().ShouldBe(Error.NotFound);
            response["error"]["message"].ToString().ShouldBe(Error.Message[Error.NotFound]);

            addressInfo = "invalid address";
            var response1 = await JsonCallAsJObject("/chain", "GetFileDescriptorSet",
                new {address = addressInfo});
            response1["error"]["code"].To<long>().ShouldBe(Error.NotFound);
            response1["error"]["message"].ToString().ShouldBe(Error.Message[Error.NotFound]);
        }
        #endregion

        #region Net cases

        [Fact]
        public async Task Net_Get_And_AddPeer()
        {
            string addressInfo = "127.0.0.1:6810";
            var response = await JsonCallAsJObject("/net", "AddPeer",
                new {address = addressInfo});
            response.ShouldNotBeNull();
            response["result"].To<bool>().ShouldBeFalse(); //currently network service is mocked.

            var response1 = await JsonCallAsJObject("/net", "GetPeers");
            response1.ShouldNotBeNull();
            response1["result"].ToList().Count.ShouldBeGreaterThanOrEqualTo(0);

            var response2 = await JsonCallAsync("/net", "RemovePeer",
                new {address = addressInfo});
        }

        #endregion

        private List<Transaction> GenerateTwoInitializeTransaction()
        {
            var transactionList = new List<Transaction>();
            var newUserKeyPair = CryptoHelper.GenerateKeyPair();

            for (int i = 0; i < 2; i++)
            {
                var transaction = _osTestHelper.GenerateTransaction(Address.FromPublicKey(newUserKeyPair.PublicKey),
                    _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name),
                    nameof(TokenContractContainer.TokenContractStub.Create), new CreateInput
                    {
                        Symbol = $"ELF",
                        TokenName= $"elf token {i}",
                        TotalSupply = 1000_0000,
                        Decimals = 2,
                        Issuer = SampleAddress.AddressList[0],
                        IsBurnable = true
                    });

                var signature =
                    CryptoHelper.SignWithPrivateKey(newUserKeyPair.PrivateKey, transaction.GetHash().ToByteArray());
                transaction.Signature = ByteString.CopyFrom(signature);

                transactionList.Add(transaction); 
            }
            
            return transactionList;
        }
    }
}