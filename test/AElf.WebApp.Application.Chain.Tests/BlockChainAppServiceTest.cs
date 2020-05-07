using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Acs0;
using AElf.Contracts.Deployer;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Vote;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.Token;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS;
using AElf.Runtime.CSharp;
using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using Google.Protobuf;
using Org.BouncyCastle.Utilities.Encoders;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using SampleAddress = AElf.Kernel.SampleAddress;

namespace AElf.WebApp.Application.Chain.Tests
{
    public sealed class BlockChainAppServiceTest : WebAppTestBase
    {
        private IReadOnlyDictionary<string, byte[]> _codes;

        public IReadOnlyDictionary<string, byte[]> Codes =>
            _codes ?? (_codes = ContractsDeployer.GetContractCodes<BlockChainAppServiceTest>());

        private byte[] ConfigurationContractCode => Codes.Single(kv => kv.Key.Contains("Configuration")).Value;
        private byte[] VoteContractCode => Codes.Single(kv => kv.Key.Contains("Vote")).Value;
        
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITxHub _txHub;
        private readonly IBlockchainStateService _blockchainStateService;
        private readonly IBlockchainStateManager _blockchainStateManager;
        private readonly IBlockStateSetManger _blockStateSetManger;
        private readonly OSTestHelper _osTestHelper;
        private readonly IAccountService _accountService;

        public BlockChainAppServiceTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _txHub = GetRequiredService<ITxHub>();
            _blockchainStateService = GetRequiredService<IBlockchainStateService>();
            _blockchainStateManager = GetRequiredService<IBlockchainStateManager>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _accountService = GetRequiredService<IAccountService>();
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
        }

        [Fact]
        public async Task Deploy_Contract_Success_Test()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            var transaction = new Transaction
            {
                From = accountAddress,
                To = _smartContractAddressService.GetZeroSmartContractAddress(),
                MethodName = nameof(BasicContractZero.DeploySmartContract),
                Params = ByteString.CopyFrom(new ContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(VoteContractCode)
                }.ToByteArray()),
                RefBlockNumber = chain.BestChainHeight,
                RefBlockPrefix = BlockHelper.GetRefBlockPrefix(chain.BestChainHash),
            };
            transaction.Signature =
                ByteString.CopyFrom(await _accountService.SignAsync(transaction.GetHash().ToByteArray()));

            var parameters = new Dictionary<string, string>
            {
                {"rawTransaction", transaction.ToByteArray().ToHex()}
            };

            var sendTransactionResponse =
                await PostResponseAsObjectAsync<SendTransactionOutput>("/api/blockChain/sendTransaction",
                    parameters);

            sendTransactionResponse.TransactionId.ShouldBe(transaction.GetHash().ToHex());
            await _osTestHelper.MinedOneBlock();
            var transactionResult = await _osTestHelper.GetTransactionResultsAsync(transaction.GetHash());
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        
        [Fact]
        public async Task Update_Contract_Success_Test()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            var deployTransaction = new Transaction
            {
                From = accountAddress,
                To = _smartContractAddressService.GetZeroSmartContractAddress(),
                MethodName = nameof(BasicContractZero.DeploySmartContract),
                Params = ByteString.CopyFrom(new ContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(VoteContractCode)
                }.ToByteArray()),
                RefBlockNumber = chain.BestChainHeight,
                RefBlockPrefix = BlockHelper.GetRefBlockPrefix(chain.BestChainHash),
            };
            deployTransaction.Signature =
                ByteString.CopyFrom(await _accountService.SignAsync(deployTransaction.GetHash().ToByteArray()));

            var parameters = new Dictionary<string, string>
            {
                {"rawTransaction", deployTransaction.ToByteArray().ToHex()}
            };

            var sendTransactionResponse =
                await PostResponseAsObjectAsync<SendTransactionOutput>("/api/blockChain/sendTransaction",
                    parameters);

            sendTransactionResponse.TransactionId.ShouldBe(deployTransaction.GetHash().ToHex());
            var deployBlock = await _osTestHelper.MinedOneBlock();
            var transactionResult = await _osTestHelper.GetTransactionResultsAsync(deployTransaction.GetHash());
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var address = Address.Parser.ParseFrom(transactionResult.ReturnValue);
            var transaction = new Transaction
            {
                From = accountAddress,
                To = address,
                MethodName = nameof(VoteContractContainer.VoteContractStub.GetVotingResult),
                Params = ByteString.CopyFrom(new GetVotingResultInput
                {
                    SnapshotNumber = 1,
                    VotingItemId = Hash.Empty
                }.ToByteArray()),
                RefBlockNumber = chain.BestChainHeight,
                RefBlockPrefix = BlockHelper.GetRefBlockPrefix(chain.BestChainHash),
            };
            transaction.Signature =
                ByteString.CopyFrom(await _accountService.SignAsync(transaction.GetHash().ToByteArray()));

            parameters = new Dictionary<string, string>
            {
                {"rawTransaction", transaction.ToByteArray().ToHex()}
            };

            sendTransactionResponse =
                await PostResponseAsObjectAsync<SendTransactionOutput>("/api/blockChain/sendTransaction",
                    parameters);

            sendTransactionResponse.TransactionId.ShouldBe(transaction.GetHash().ToHex());
            await _osTestHelper.MinedOneBlock();
            var response = await GetResponseAsObjectAsync<TransactionResultDto>(
                $"/api/blockChain/transactionResult?transactionId={transaction.GetHash().ToHex()}");
            response.Status.ShouldBe(TransactionResultStatus.Mined.ToString().ToUpper());
            response.Transaction.Params.ShouldBe(GetVotingResultInput.Parser.ParseFrom(transaction.Params).ToString());
            
            var updateTransaction = new Transaction
            {
                From = accountAddress,
                To = _smartContractAddressService.GetZeroSmartContractAddress(),
                MethodName = nameof(BasicContractZero.UpdateSmartContract),
                Params = ByteString.CopyFrom(new ContractUpdateInput
                {
                    Address = address,
                    Code = ByteString.CopyFrom(ConfigurationContractCode)
                }.ToByteArray()),
                RefBlockNumber = chain.BestChainHeight,
                RefBlockPrefix = BlockHelper.GetRefBlockPrefix(chain.BestChainHash),
            };
            updateTransaction.Signature =
                ByteString.CopyFrom(await _accountService.SignAsync(updateTransaction.GetHash().ToByteArray()));
            
            parameters = new Dictionary<string, string>
            {
                {"rawTransaction", updateTransaction.ToByteArray().ToHex()}
            };

            sendTransactionResponse =
                await PostResponseAsObjectAsync<SendTransactionOutput>("/api/blockChain/sendTransaction",
                    parameters);

            sendTransactionResponse.TransactionId.ShouldBe(updateTransaction.GetHash().ToHex());
            await _osTestHelper.MinedOneBlock();
            transactionResult = await _osTestHelper.GetTransactionResultsAsync(updateTransaction.GetHash());
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            response = await GetResponseAsObjectAsync<TransactionResultDto>(
                $"/api/blockChain/transactionResult?transactionId={transaction.GetHash().ToHex()}");
            response.Status.ShouldBe(TransactionResultStatus.Mined.ToString().ToUpper());
            response.Transaction.Params.ShouldBe(transaction.Params.ToBase64());
            response.Transaction.Params.ShouldNotBe(
                GetVotingResultInput.Parser.ParseFrom(transaction.Params).ToString());
        }

        [Fact]
        public async Task GetBlockHeight_Test()
        {
            // Get current height
            var response = await GetResponseAsStringAsync("/api/blockChain/blockHeight");
            var currentHeight = long.Parse(response);

            var chain = await _blockchainService.GetChainAsync();
            currentHeight.ShouldBe(chain.BestChainHeight);

            // Mined one block
            var transaction = await _osTestHelper.GenerateTransferTransaction();
            await _osTestHelper.BroadcastTransactions(new List<Transaction> {transaction});
            await _osTestHelper.MinedOneBlock();

            // Get latest height
            response = await GetResponseAsStringAsync("/api/blockChain/blockHeight");
            var height = long.Parse(response);
            height.ShouldBe(currentHeight + 1);
        }

        [Fact]
        public async Task ExecuteTransaction_Success_Test()
        {
            // Generate a transaction
            var transaction = await GenerateViewTransaction(
                nameof(TokenContractContainer.TokenContractStub.GetTokenInfo),
                new GetTokenInfoInput
                {
                    Symbol = "ELF"
                });

            var parameters = new Dictionary<string, string>
            {
                {"rawTransaction", transaction.ToByteArray().ToHex()}
            };

            var response = await PostResponseAsStringAsync("/api/blockChain/executeTransaction", parameters);
            response.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async Task ExecuteTransaction_Failed_Test()
        {
            var parameters = new Dictionary<string, string>
            {
                {
                    "rawTransaction",
                    "0a200a1e4604ccbdaa377fd7022b56436b99309e8b71cc5d78e909d271dbd1aeee6412200a1eaaa58b6cf58d4ef337f6dc55b701fd57d622015a3548a91a4e40892aa355180b220436957f93320c476574546f6b656e496e666f3a060a04454c46324a416246d781d80759d8ae6bb895b17203a3c9d4e89f083d7d89d9b6cbbf1c67ded52e134108fc8b3646f6549313868ce3e68a7117815cc0c2107ef1a986430a12ba002"
                }
            };
            var response =
                await PostResponseAsObjectAsync<WebAppErrorResponse>("/api/blockChain/executeTransaction", parameters,
                    expectedStatusCode: HttpStatusCode.Forbidden);

            response.Error.Code.ShouldBe(Error.InvalidParams.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.InvalidParams]);

            //invalid signature
            var transaction = await GenerateViewTransaction(
                nameof(TokenContractContainer.TokenContractStub.GetTokenInfo),
                new GetTokenInfoInput
                {
                    Symbol = "ELF"
                });
            transaction.Signature = ByteString.CopyFromUtf8("invalid");

            parameters = new Dictionary<string, string>
            {
                {
                    "rawTransaction", transaction.ToByteArray().ToHex()
                }
            };
            response =
                await PostResponseAsObjectAsync<WebAppErrorResponse>("/api/blockChain/executeTransaction", parameters,
                    expectedStatusCode: HttpStatusCode.Forbidden);
            response.Error.Code.ShouldBe(Error.InvalidSignature.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.InvalidSignature]);
        }

        [Fact]
        public async Task ExecuteRawTransaction_Success_Test()
        {
            const string methodName = "GetBalance";
            var contractAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(await _osTestHelper.GetChainContextAsync(), TokenSmartContractAddressNameProvider.StringName);
            var chain = await _blockchainService.GetChainAsync();
            var accountAddress = await _accountService.GetAccountAsync();
            var toAddress = Base64.ToBase64String(accountAddress.Value.ToByteArray());
            var parameters = new Dictionary<string, string>
            {
                {"From", accountAddress.ToBase58()},
                {"To", contractAddress.ToBase58()},
                {"RefBlockNumber", chain.BestChainHeight.ToString()},
                {"RefBlockHash", chain.BestChainHash.ToHex()},
                {"MethodName", methodName},
                {"Params", "{\"owner\":{ \"value\": \"" + toAddress + "\" },\"symbol\":\"ELF\"}"}
            };
            var createTransactionResponse =
                await PostResponseAsObjectAsync<CreateRawTransactionOutput>("/api/blockChain/rawTransaction",
                    parameters);
            var transactionId =
                HashHelper.ComputeFrom(ByteArrayHelper.HexStringToByteArray(createTransactionResponse.RawTransaction));

            var signature = await _accountService.SignAsync(transactionId.ToByteArray());
            parameters = new Dictionary<string, string>
            {
                {"RawTransaction", createTransactionResponse.RawTransaction},
                {"Signature", signature.ToHex()}
            };
            var sendTransactionResponse =
                await PostResponseAsObjectAsync<string>("/api/blockChain/executeRawTransaction",
                    parameters);
            var output = new GetBalanceOutput
            {
                Owner = accountAddress,
                Symbol = "ELF",
                Balance = _osTestHelper.TokenTotalSupply - _osTestHelper.MockChainTokenAmount
            };
            sendTransactionResponse.ShouldBe(output.ToString());
        }

        [Fact]
        public async Task ExecuteRawTransaction_Failed_Test()
        {
            var parameters = new Dictionary<string, string>
            {
                {"RawTransaction", "wrongTransaction"}
            };
            var wrongTransactionResponse =
                await PostResponseAsObjectAsync<WebAppErrorResponse>("/api/blockChain/executeRawTransaction",
                    parameters, expectedStatusCode: HttpStatusCode.Forbidden);
            wrongTransactionResponse.Error.Code.ShouldBe(Error.InvalidParams.ToString());
            wrongTransactionResponse.Error.Message.ShouldBe(Error.Message[Error.InvalidParams]);

            const string methodName = "GetBalance";
            var contractAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(await _osTestHelper.GetChainContextAsync(), TokenSmartContractAddressNameProvider.StringName);
            var chain = await _blockchainService.GetChainAsync();
            var accountAddress = await _accountService.GetAccountAsync();
            var toAddress = Base64.ToBase64String(accountAddress.Value.ToByteArray());
            parameters = new Dictionary<string, string>
            {
                {"From", accountAddress.ToBase58()},
                {"To", contractAddress.ToBase58()},
                {"RefBlockNumber", chain.BestChainHeight.ToString()},
                {"RefBlockHash", chain.BestChainHash.ToHex()},
                {"MethodName", methodName},
                {"Params", "{\"owner\":{ \"value\": \"" + toAddress + "\" },\"symbol\":\"ELF\"}"}
            };
            var createTransactionResponse =
                await PostResponseAsObjectAsync<CreateRawTransactionOutput>("/api/blockChain/rawTransaction",
                    parameters);
            var wrongSignature = ByteString.CopyFromUtf8("wrongSignature").ToByteArray().ToHex();
            parameters = new Dictionary<string, string>
            {
                {"RawTransaction", createTransactionResponse.RawTransaction},
                {"Signature", wrongSignature}
            };
            var wrongSignatureResponse =
                await PostResponseAsObjectAsync<WebAppErrorResponse>("/api/blockChain/executeRawTransaction",
                    parameters, expectedStatusCode: HttpStatusCode.Forbidden);
            wrongSignatureResponse.Error.Code.ShouldBe(Error.InvalidSignature.ToString());
            wrongSignatureResponse.Error.Message.ShouldBe(Error.Message[Error.InvalidSignature]);
        }

        [Fact]
        public async Task Send_Transaction_Success_Test()
        {
            // Generate a transaction
            var transaction = await _osTestHelper.GenerateTransferTransaction();
            var transactionId = transaction.GetHash();

            var parameters = new Dictionary<string, string>
            {
                {"rawTransaction", transaction.ToByteArray().ToHex()}
            };

            var sendTransactionResponse =
                await PostResponseAsObjectAsync<SendTransactionOutput>("/api/blockChain/sendTransaction",
                    parameters);

            sendTransactionResponse.TransactionId.ShouldBe(transactionId.ToHex());

            var existTransaction = await _txHub.GetExecutableTransactionSetAsync();
            existTransaction.Transactions[0].GetHash().ShouldBe(transactionId);
        }

        [Fact]
        public async Task Send_Transaction_ReturnInvalidTransaction_Test()
        {
            var fakeTransaction = "FakeTransaction";
            var parameters = new Dictionary<string, string>
            {
                {"rawTransaction", fakeTransaction}
            };
            var response =
                await PostResponseAsObjectAsync<WebAppErrorResponse>("/api/blockChain/sendTransaction",
                    parameters, expectedStatusCode: HttpStatusCode.Forbidden);

            response.Error.Code.ShouldBe(Error.InvalidTransaction.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.InvalidTransaction]);

            var contractAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(await _osTestHelper.GetChainContextAsync(), TokenSmartContractAddressNameProvider.StringName);

            var from = Base64.ToBase64String(Encoding.UTF8.GetBytes("InvalidAddress"));
            var to = Base64.ToBase64String(contractAddress.Value.ToByteArray());
            var json =
                "{ \"from\": { \"value\": \"" + from + "\" }, \"to\": { \"value\": \"" + to +
                "\" }, \"ref_block_number\": \"11\", \"ref_block_prefix\": \"H9f1zQ==\", \"method_name\": \"Transfer\", \"params\": \"CiIKIDAK0LTy1ZAHaf1nAnq/gkSqTCs4Kh6czxWpbNEX4EwaEgNFTEYYFA==\"}";
            var transaction = Transaction.Parser.ParseJson(json);

            var signature = await _accountService.SignAsync(transaction.GetHash().ToByteArray());
            transaction.Signature = ByteString.CopyFrom(signature);
            parameters = new Dictionary<string, string>
            {
                {"rawTransaction", transaction.ToByteArray().ToHex()}
            };
            response =
                await PostResponseAsObjectAsync<WebAppErrorResponse>("/api/blockChain/sendTransaction",
                    parameters, expectedStatusCode: HttpStatusCode.Forbidden);

            response.Error.Code.ShouldBe(Error.InvalidTransaction.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.InvalidTransaction]);
        }

        [Fact]
        public async Task Send_Transaction_ReturnNoMatchMethodInContractAddress_Test()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var from = Base64.ToBase64String(accountAddress.Value.ToByteArray());
            var contractAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(await _osTestHelper.GetChainContextAsync(), TokenSmartContractAddressNameProvider.StringName);
            var to = Base64.ToBase64String(contractAddress.Value.ToByteArray());
            var json =
                "{ \"from\": { \"value\": \"" + from + "\" }, \"to\": { \"value\": \"" + to +
                "\" }, \"ref_block_number\": \"11\", \"ref_block_prefix\": \"H9f1zQ==\", \"method_name\": \"InvalidMethod\", \"params\": \"CiIKIDAK0LTy1ZAHaf1nAnq/gkSqTCs4Kh6czxWpbNEX4EwaEgNFTEYYFA==\"}";
            var transaction = Transaction.Parser.ParseJson(json);

            var signature = await _accountService.SignAsync(transaction.GetHash().ToByteArray());
            transaction.Signature = ByteString.CopyFrom(signature);
            var parameters = new Dictionary<string, string>
            {
                {"rawTransaction", transaction.ToByteArray().ToHex()}
            };
            var response =
                await PostResponseAsObjectAsync<WebAppErrorResponse>("/api/blockChain/sendTransaction",
                    parameters, expectedStatusCode: HttpStatusCode.Forbidden);

            response.Error.Code.ShouldBe(Error.NoMatchMethodInContractAddress.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.NoMatchMethodInContractAddress]);
        }

        [Fact]
        public async Task Send_Transaction_ReturnInvalidParams_Test()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var from = Base64.ToBase64String(accountAddress.Value.ToByteArray());
            var contractAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(await _osTestHelper.GetChainContextAsync(), TokenSmartContractAddressNameProvider.StringName);
            var to = Base64.ToBase64String(contractAddress.Value.ToByteArray());
            var transactionParams = TransferInput.Parser.ParseJson(
                "{\"to\":{ \"value\": \"" + Base64.ToBase64String(Encoding.UTF8.GetBytes("InvalidAddress")) +
                "\" },\"symbol\":\"ELF\",\"amount\":100,\"memo\":\"test\"}");

            var json = "{ \"from\": { \"value\": \"" + from + "\" }, \"to\": { \"value\": \"" + to +
                       "\" }, \"ref_block_number\": \"11\", \"ref_block_prefix\": \"H9f1zQ==\", \"method_name\": \"Transfer\", \"params\": \"" +
                       Base64.ToBase64String(transactionParams.ToByteArray()) + "\" }";
            var transaction = Transaction.Parser.ParseJson(json);

            var signature = await _accountService.SignAsync(transaction.GetHash().ToByteArray());
            transaction.Signature = ByteString.CopyFrom(signature);
            var parameters = new Dictionary<string, string>
            {
                {"rawTransaction", transaction.ToByteArray().ToHex()}
            };
            var response =
                await PostResponseAsObjectAsync<WebAppErrorResponse>("/api/blockChain/sendTransaction",
                    parameters, expectedStatusCode: HttpStatusCode.Forbidden);

            response.Error.Code.ShouldBe(Error.InvalidParams.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.InvalidParams]);
        }

        [Fact]
        public async Task Send_UnableVerify_Transaction_ReturnInvalidTransaction_Test()
        {
            // Generate unsigned transaction
            var transaction = await _osTestHelper.GenerateTransferTransaction();
            transaction.Signature = ByteString.CopyFrom(new byte[0]);

            var parameters = new Dictionary<string, string>
            {
                {"rawTransaction", transaction.ToByteArray().ToHex()}
            };
            var response = await PostResponseAsObjectAsync<WebAppErrorResponse>(
                "/api/blockChain/sendTransaction", parameters, expectedStatusCode: HttpStatusCode.Forbidden);

            response.Error.Code.ShouldBe(Error.InvalidSignature.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.InvalidSignature]);

            var existTransaction = await _txHub.GetQueuedTransactionAsync(transaction.GetHash());
            existTransaction.ShouldBeNull();
        }

        [Fact]
        public async Task Send_Transactions_Success_Test()
        {
            // Generate two transactions
            var transaction1 = await _osTestHelper.GenerateTransferTransaction();
            var transaction2 = await _osTestHelper.GenerateTransferTransaction();
            var transactions = new List<Transaction> {transaction1, transaction2};
            var rawTransactions = string.Join(',', transactions.Select(t => t.ToByteArray().ToHex()));

            var parameters = new Dictionary<string, string>
            {
                {"rawTransactions", rawTransactions}
            };
            var sendTransactionsResponse =
                await PostResponseAsObjectAsync<string[]>("/api/blockChain/sendTransactions", parameters);
            var responseTransactionIds = sendTransactionsResponse.ToList();

            responseTransactionIds.Count.ShouldBe(2);

            var existTransaction = await _txHub.GetExecutableTransactionSetAsync();
            existTransaction.Transactions.Select(x => x.GetHash().ToHex()).ShouldContain(responseTransactionIds[0]);
            existTransaction.Transactions.Select(x => x.GetHash().ToHex()).ShouldContain(responseTransactionIds[1]);

            var getTransactionPoolStatusResponse =
                await GetResponseAsObjectAsync<GetTransactionPoolStatusOutput>("/api/blockChain/transactionPoolStatus");
            getTransactionPoolStatusResponse.Queued.ShouldBe(2);
        }

        [Fact]
        public async Task Send_Transactions_ReturnInvalidTransaction_Test()
        {
            var transaction1 = await _osTestHelper.GenerateTransferTransaction();
            var transaction2 = await _osTestHelper.GenerateTransferTransaction();
            var contractAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(await _osTestHelper.GetChainContextAsync(), TokenSmartContractAddressNameProvider.StringName);

            var from = Base64.ToBase64String(Encoding.UTF8.GetBytes("InvalidAddress"));
            var to = Base64.ToBase64String(contractAddress.Value.ToByteArray());
            var json =
                "{ \"from\": { \"value\": \"" + from + "\" }, \"to\": { \"value\": \"" + to +
                "\" }, \"ref_block_number\": \"11\", \"ref_block_prefix\": \"H9f1zQ==\", \"method_name\": \"Transfer\", \"params\": \"CiIKIDAK0LTy1ZAHaf1nAnq/gkSqTCs4Kh6czxWpbNEX4EwaEgNFTEYYFA==\"}";
            var transaction3 = Transaction.Parser.ParseJson(json);

            var signature = await _accountService.SignAsync(transaction3.GetHash().ToByteArray());
            transaction3.Signature = ByteString.CopyFrom(signature);
            var transactions = new List<Transaction> {transaction1, transaction2, transaction3};
            var rawTransactions = string.Join(',', transactions.Select(t => t.ToByteArray().ToHex()));

            var parameters = new Dictionary<string, string>
            {
                {"rawTransactions", rawTransactions}
            };
            var sendTransactionsResponse =
                await PostResponseAsObjectAsync<WebAppErrorResponse>("/api/blockChain/sendTransactions",
                    parameters, expectedStatusCode: HttpStatusCode.Forbidden);
            sendTransactionsResponse.Error.Code.ShouldBe(Error.InvalidTransaction.ToString());
            sendTransactionsResponse.Error.Message.ShouldBe(Error.Message[Error.InvalidTransaction]);
        }

        [Fact]
        public async Task Send_Transactions_ReturnNoMatchMethodInContractAddress_Test()
        {
            var transaction1 = await _osTestHelper.GenerateTransferTransaction();
            var transaction2 = await _osTestHelper.GenerateTransferTransaction();

            var accountAddress = await _accountService.GetAccountAsync();
            var from = Base64.ToBase64String(accountAddress.Value.ToByteArray());
            var contractAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(await _osTestHelper.GetChainContextAsync(), TokenSmartContractAddressNameProvider.StringName);
            var to = Base64.ToBase64String(contractAddress.Value.ToByteArray());
            var json =
                "{ \"from\": { \"value\": \"" + from + "\" }, \"to\": { \"value\": \"" + to +
                "\" }, \"ref_block_number\": \"11\", \"ref_block_prefix\": \"H9f1zQ==\", \"method_name\": \"InvalidMethod\", \"params\": \"CiIKIDAK0LTy1ZAHaf1nAnq/gkSqTCs4Kh6czxWpbNEX4EwaEgNFTEYYFA==\"}";
            var transaction3 = Transaction.Parser.ParseJson(json);

            var signature = await _accountService.SignAsync(transaction3.GetHash().ToByteArray());
            transaction3.Signature = ByteString.CopyFrom(signature);
            var transactions = new List<Transaction> {transaction1, transaction2, transaction3};
            var rawTransactions = string.Join(',', transactions.Select(t => t.ToByteArray().ToHex()));

            var parameters = new Dictionary<string, string>
            {
                {"rawTransactions", rawTransactions}
            };
            var sendTransactionsResponse =
                await PostResponseAsObjectAsync<WebAppErrorResponse>("/api/blockChain/sendTransactions",
                    parameters, expectedStatusCode: HttpStatusCode.Forbidden);
            sendTransactionsResponse.Error.Code.ShouldBe(Error.NoMatchMethodInContractAddress.ToString());
            sendTransactionsResponse.Error.Message.ShouldBe(Error.Message[Error.NoMatchMethodInContractAddress]);
        }

        [Fact]
        public async Task Send_Transactions_ReturnInvalidParams_Test()
        {
            var transaction1 = await _osTestHelper.GenerateTransferTransaction();
            var transaction2 = await _osTestHelper.GenerateTransferTransaction();

            var accountAddress = await _accountService.GetAccountAsync();
            var from = Base64.ToBase64String(accountAddress.Value.ToByteArray());
            var contractAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(await _osTestHelper.GetChainContextAsync(), TokenSmartContractAddressNameProvider.StringName);
            var to = Base64.ToBase64String(contractAddress.Value.ToByteArray());
            var transactionParams = TransferInput.Parser.ParseJson(
                "{\"to\":{ \"value\": \"" + Base64.ToBase64String(Encoding.UTF8.GetBytes("InvalidAddress")) +
                "\" },\"symbol\":\"ELF\",\"amount\":100,\"memo\":\"test\"}");

            var json = "{ \"from\": { \"value\": \"" + from + "\" }, \"to\": { \"value\": \"" + to +
                       "\" }, \"ref_block_number\": \"11\", \"ref_block_prefix\": \"H9f1zQ==\", \"method_name\": \"Transfer\", \"params\": \"" +
                       Base64.ToBase64String(transactionParams.ToByteArray()) + "\" }";
            var transaction3 = Transaction.Parser.ParseJson(json);

            var signature = await _accountService.SignAsync(transaction3.GetHash().ToByteArray());
            transaction3.Signature = ByteString.CopyFrom(signature);
            var transactions = new List<Transaction> {transaction1, transaction2, transaction3};
            var rawTransactions = string.Join(',', transactions.Select(t => t.ToByteArray().ToHex()));

            var parameters = new Dictionary<string, string>
            {
                {"rawTransactions", rawTransactions}
            };
            var sendTransactionsResponse =
                await PostResponseAsObjectAsync<WebAppErrorResponse>("/api/blockChain/sendTransactions",
                    parameters, expectedStatusCode: HttpStatusCode.Forbidden);
            sendTransactionsResponse.Error.Code.ShouldBe(Error.InvalidParams.ToString());
            sendTransactionsResponse.Error.Message.ShouldBe(Error.Message[Error.InvalidParams]);
        }

        [Fact]
        public async Task Get_TransactionResult_Success_Test()
        {
            // Generate a transaction and broadcast
            var transaction = await _osTestHelper.GenerateTransferTransaction();
            var transactionHex = transaction.GetHash().ToHex();
            await _osTestHelper.BroadcastTransactions(new List<Transaction> {transaction});

            // Before mined
            var response = await GetResponseAsObjectAsync<TransactionResultDto>(
                $"/api/blockChain/transactionResult?transactionId={transactionHex}");

            response.TransactionId.ShouldBe(transactionHex);
            response.Status.ShouldBe(TransactionResultStatus.Pending.ToString().ToUpper());

            await _osTestHelper.MinedOneBlock();

            // After mined
            response = await GetResponseAsObjectAsync<TransactionResultDto>(
                $"/api/blockChain/transactionResult?transactionId={transactionHex}");

            response.TransactionId.ShouldBe(transactionHex);
            response.Status.ShouldBe(TransactionResultStatus.Mined.ToString().ToUpper());
            response.TransactionSize.ShouldBe(transaction.CalculateSize());
        }

        [Fact]
        public async Task Get_Failed_TransactionResult_Success_Test()
        {
            // Generate a transaction and broadcast
            var transactionList = await GenerateTwoInitializeTransaction();
            await _osTestHelper.BroadcastTransactions(transactionList);

            var block = await _osTestHelper.MinedOneBlock();

            // After executed
            var transactionHex = transactionList[1].GetHash().ToHex();
            var response = await GetResponseAsObjectAsync<TransactionResultDto>(
                $"/api/blockChain/transactionResult?transactionId={transactionHex}");

            response.TransactionId.ShouldBe(transactionHex);
            response.BlockNumber.ShouldBe(block.Height);
            response.BlockHash.ShouldBe(block.Header.GetHash().ToHex());
            response.Status.ShouldBe(TransactionResultStatus.Failed.ToString().ToUpper());
        }

        [Fact]
        public async Task Get_NotExisted_TransactionResult_Test()
        {
            // Generate a transaction
            var transaction = await _osTestHelper.GenerateTransferTransaction();
            var transactionHex = transaction.GetHash().ToHex();

            var response = await GetResponseAsObjectAsync<TransactionResultDto>(
                $"/api/blockChain/transactionResult?transactionId={transactionHex}");

            response.TransactionId.ShouldBe(transactionHex);
            response.Status.ShouldBe(TransactionResultStatus.NotExisted.ToString());
        }

        [Fact]
        public async Task Get_TransactionResult_ReturnInvalidTransactionId_Test()
        {
            var fakeTransactionId = "FakeTransactionId";
            var response = await GetResponseAsObjectAsync<WebAppErrorResponse>(
                $"/api/blockChain/transactionResult?transactionId={fakeTransactionId}",
                expectedStatusCode: HttpStatusCode.Forbidden);

            response.Error.Code.ShouldBe(Error.InvalidTransactionId.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.InvalidTransactionId]);
        }

        [Fact]
        public async Task Get_TransactionResults_Success_Test()
        {
            // Generate 20 transactions and mined
            var transactions = new List<Transaction>();
            for (int i = 0; i < 20; i++)
            {
                transactions.Add(await _osTestHelper.GenerateTransferTransaction());
            }

            await _osTestHelper.BroadcastTransactions(transactions);
            var block = await _osTestHelper.MinedOneBlock();

            var response = await GetResponseAsObjectAsync<List<TransactionResultDto>>(
                $"/api/blockChain/transactionResults?blockHash={block.GetHash().ToHex()}&offset=0&limit=15");

            response.Count.ShouldBe(15);
            foreach (var transactionResultDto in response)
            {
                transactionResultDto.TransactionSize.ShouldBe(transactions
                    .Single(t => t.GetHash() == Hash.LoadFromHex(transactionResultDto.TransactionId)).CalculateSize());
            }

            response = await GetResponseAsObjectAsync<List<TransactionResultDto>>(
                $"/api/blockChain/transactionResults?blockHash={block.GetHash().ToHex()}&offset=15&limit=15");

            response.Count.ShouldBe(5);
            foreach (var transactionResultDto in response)
            {
                transactionResultDto.TransactionSize.ShouldBe(transactions
                    .Single(t => t.GetHash() == Hash.LoadFromHex(transactionResultDto.TransactionId)).CalculateSize());
            }
        }

        [Fact]
        public async Task Get_NotExisted_TransactionResults_Test()
        {
            var block = _osTestHelper.GenerateBlock(Hash.Empty, 10);
            var blockHash = block.GetHash().ToHex();
            var response = await GetResponseAsObjectAsync<WebAppErrorResponse>(
                $"/api/blockChain/transactionResults?blockHash={blockHash}&offset=0&limit=10",
                expectedStatusCode: HttpStatusCode.Forbidden);

            response.Error.Code.ShouldBe(Error.NotFound.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.NotFound]);
        }

        [Fact]
        public async Task Get_TransactionResults_With_InvalidParameter_Test()
        {
            var block = _osTestHelper.GenerateBlock(Hash.Empty, 10);
            var blockHash = block.GetHash().ToHex();

            var response1 = await GetResponseAsObjectAsync<WebAppErrorResponse>(
                $"/api/blockChain/transactionResults?blockHash={blockHash}&offset=-3&limit=10",
                expectedStatusCode: HttpStatusCode.Forbidden);

            response1.Error.Code.ShouldBe(Error.InvalidOffset.ToString());
            response1.Error.Message.Contains("Offset must greater than or equal to 0").ShouldBeTrue();

            var response2 = await GetResponseAsObjectAsync<WebAppErrorResponse>(
                $"/api/blockChain/transactionResults?blockHash={blockHash}&offset=0&limit=-5",
                expectedStatusCode: HttpStatusCode.Forbidden);
            response2.Error.Code.ShouldBe(Error.InvalidLimit.ToString());
            response2.Error.Message.Contains("Limit must between 0 and 100").ShouldBeTrue();

            var response3 = await GetResponseAsObjectAsync<WebAppErrorResponse>(
                $"/api/blockChain/transactionResults?blockHash={blockHash}&offset=0&limit=120",
                expectedStatusCode: HttpStatusCode.Forbidden);
            response3.Error.Code.ShouldBe(Error.InvalidLimit.ToString());
            response3.Error.Message.Contains("Limit must between 0 and 100").ShouldBeTrue();
            
            var response4 = await GetResponseAsObjectAsync<WebAppErrorResponse>(
                $"/api/blockChain/transactionResults?blockHash={blockHash.Substring(2)}&offset=0&limit=20",
                expectedStatusCode: HttpStatusCode.Forbidden);
            response4.Error.Code.ShouldBe(Error.InvalidBlockHash.ToString());
        }

        [Fact]
        public async Task Get_Block_By_BlockHeight_Success_Test()
        {
            var chain = await _blockchainService.GetChainAsync();
            var transactions = new List<Transaction>();
            for (int i = 0; i < 3; i++)
            {
                transactions.Add(await _osTestHelper.GenerateTransferTransaction());
            }

            await _osTestHelper.BroadcastTransactions(transactions);
            var block = await _osTestHelper.MinedOneBlock();

            var response =
                await GetResponseAsObjectAsync<BlockDto>(
                    "/api/blockChain/blockByHeight?blockHeight=12&includeTransactions=true");

            response.BlockHash.ShouldBe(block.GetHash().ToHex());
            response.Header.PreviousBlockHash.ShouldBe(block.Header.PreviousBlockHash.ToHex());
            response.Header.MerkleTreeRootOfTransactions.ShouldBe(block.Header.MerkleTreeRootOfTransactions.ToHex());
            response.Header.MerkleTreeRootOfWorldState.ShouldBe(block.Header.MerkleTreeRootOfWorldState.ToHex());
            response.Header.Height.ShouldBe(block.Height);
            response.Header.Time.ShouldBe(block.Header.Time.ToDateTime());
            response.Header.ChainId.ShouldBe(ChainHelper.ConvertChainIdToBase58(chain.Id));
            response.Header.Bloom.ShouldBe(block.Header.Bloom.ToBase64());
            response.Header.Extra.ShouldBe(block.Header.ExtraData?.ToString());
            response.Header.MerkleTreeRootOfTransactionState.ShouldBe(block.Header.MerkleTreeRootOfTransactionStatus.ToHex());
            response.Header.SignerPubkey.ShouldBe(block.Header.SignerPubkey.ToHex());
            response.Body.TransactionsCount.ShouldBe(3);
            response.BlockSize.ShouldBe(block.CalculateSize());

            var responseTransactions = response.Body.Transactions;
            responseTransactions.Count.ShouldBe(3);
        }

        [Fact]
        public async Task Get_Block_By_BlockHash_Success_Test()
        {
            var chain = await _blockchainService.GetChainAsync();
            var transactions = new List<Transaction>();
            for (int i = 0; i < 3; i++)
            {
                transactions.Add(await _osTestHelper.GenerateTransferTransaction());
            }

            await _osTestHelper.BroadcastTransactions(transactions);
            var block = await _osTestHelper.MinedOneBlock();

            var response =
                await GetResponseAsObjectAsync<BlockDto>(
                    $"/api/blockChain/block?blockHash={block.GetHash().ToHex()}&includeTransactions=true");

            response.BlockHash.ShouldBe(block.GetHash().ToHex());
            response.Header.PreviousBlockHash.ShouldBe(block.Header.PreviousBlockHash.ToHex());
            response.Header.MerkleTreeRootOfTransactions.ShouldBe(block.Header.MerkleTreeRootOfTransactions.ToHex());
            response.Header.MerkleTreeRootOfWorldState.ShouldBe(block.Header.MerkleTreeRootOfWorldState.ToHex());
            response.Header.Height.ShouldBe(block.Height);
            response.Header.Time.ShouldBe(block.Header.Time.ToDateTime());
            response.Header.ChainId.ShouldBe(ChainHelper.ConvertChainIdToBase58(chain.Id));
            response.Header.Bloom.ShouldBe(block.Header.Bloom.ToBase64());
            response.Header.SignerPubkey.ShouldBe(block.Header.SignerPubkey.ToByteArray().ToHex());
            response.Header.Extra.ShouldBe(block.Header.ExtraData?.ToString());
            response.Header.MerkleTreeRootOfTransactionState.ShouldBe(block.Header.MerkleTreeRootOfTransactionStatus.ToHex());
            response.Body.TransactionsCount.ShouldBe(3);
            response.BlockSize.ShouldBe(block.CalculateSize());

            var responseTransactions = response.Body.Transactions;
            responseTransactions.Count.ShouldBe(3);
        }

        [Fact]
        public async Task Get_Block_ReturnNotFound_Test()
        {
            var response = await GetResponseAsObjectAsync<WebAppErrorResponse>(
                "/api/blockChain/blockByHeight?blockHeight=0",
                expectedStatusCode: HttpStatusCode.Forbidden);
            response.Error.Code.ShouldBe(Error.NotFound.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.NotFound]);

            response = await GetResponseAsObjectAsync<WebAppErrorResponse>(
                "/api/blockChain/blockByHeight?blockHeight=100",
                expectedStatusCode: HttpStatusCode.Forbidden);
            response.Error.Code.ShouldBe(Error.NotFound.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.NotFound]);

            response = await GetResponseAsObjectAsync<WebAppErrorResponse>(
                $"/api/blockChain/block?blockHash={Hash.Empty.ToHex()}",
                expectedStatusCode: HttpStatusCode.Forbidden);
            response.Error.Code.ShouldBe(Error.NotFound.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.NotFound]);

            response = await GetResponseAsObjectAsync<WebAppErrorResponse>(
                "/api/blockChain/block?blockHash=7526008f73d931f48a9246648f3147aacf5bd9b2c79f93a708a86f77baaed865",
                expectedStatusCode: HttpStatusCode.Forbidden);
            response.Error.Code.ShouldBe(Error.NotFound.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.NotFound]);

            //invalid block hash parameter
            const string blockHash = "invalid-hash";
            response = await GetResponseAsObjectAsync<WebAppErrorResponse>(
                $"/api/blockChain/block?blockHash={blockHash}",
                expectedStatusCode: HttpStatusCode.Forbidden);
            response.Error.Code.ShouldBe(Error.InvalidBlockHash.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.InvalidBlockHash]);
        }

        [Fact]
        public async Task Get_Chain_Status_Success_Test()
        {
            var chain = await _blockchainService.GetChainAsync();
            var basicContractZero = _smartContractAddressService.GetZeroSmartContractAddress();

            var response = await GetResponseAsObjectAsync<ChainStatusDto>("/api/blockChain/chainStatus");
            response.Branches.ShouldNotBeNull();
            var responseChainId = ChainHelper.ConvertBase58ToChainId(response.ChainId);
            responseChainId.ShouldBe(chain.Id);
            response.GenesisContractAddress.ShouldBe(basicContractZero.ToBase58());
            response.BestChainHeight.ShouldBe(11);
            response.BestChainHash.ShouldBe(chain.BestChainHash?.ToHex());
            response.LongestChainHeight = chain.LongestChainHeight;
            response.LongestChainHash = chain.LongestChainHash?.ToHex();
            response.GenesisBlockHash = chain.GenesisBlockHash.ToHex();
            response.LastIrreversibleBlockHash = chain.LastIrreversibleBlockHash?.ToHex();
            response.LastIrreversibleBlockHeight = chain.LastIrreversibleBlockHeight;
        }

        [Fact]
        public async Task Get_Block_State_Success_Test()
        {
            var transactions = new List<Transaction>();
            for (int i = 0; i < 3; i++)
            {
                transactions.Add(await _osTestHelper.GenerateTransferTransaction());
            }

            await _osTestHelper.BroadcastTransactions(transactions);
            await _osTestHelper.MinedOneBlock();

            var block = await GetResponseAsObjectAsync<BlockDto>(
                "/api/blockChain/blockByHeight?blockHeight=12&includeTransactions=true");


            var blockState = await GetResponseAsObjectAsync<BlockStateDto>(
                $"/api/blockChain/blockState?blockHash={block.BlockHash}");
            blockState.ShouldNotBeNull();
            blockState.BlockHash.ShouldBe(block.BlockHash);
            blockState.BlockHeight.ShouldBe(12);
            blockState.PreviousHash.ShouldBe(block.Header.PreviousBlockHash);
            blockState.Changes.ShouldNotBeNull();
        }

        [Fact]
        public async Task Get_Block_State_FewBlocks_Later_Test()
        {
            var transactions = new List<Transaction>();
            for (int i = 0; i < 3; i++)
            {
                transactions.Add(await _osTestHelper.GenerateTransferTransaction());
            }

            await _osTestHelper.BroadcastTransactions(transactions);
            var block = await _osTestHelper.MinedOneBlock();

            //Continue generate block 
            for (int i = 0; i < 10; i++)
            {
                await _osTestHelper.MinedOneBlock();
            }

            //Check Block State
            var blockState = await GetResponseAsObjectAsync<BlockStateDto>(
                $"/api/blockChain/blockState?blockHash={block.GetHash().ToHex()}");
            blockState.ShouldNotBeNull();
            blockState.BlockHash.ShouldBe(block.GetHash().ToHex());
            blockState.BlockHeight.ShouldBe(12);
            blockState.PreviousHash.ShouldBe(block.Header.PreviousBlockHash.ToHex());
            blockState.Changes.ShouldNotBeNull();

            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            await _blockchainStateService.MergeBlockStateAsync(blockStateSet.BlockHeight,
                blockStateSet.BlockHash);

            var errorResponse = await GetResponseAsObjectAsync<WebAppErrorResponse>(
                $"/api/blockChain/blockState?blockHash={block.GetHash().ToHex()}",
                expectedStatusCode: HttpStatusCode.Forbidden);
            errorResponse.Error.Code.ShouldBe(Error.NotFound.ToString());
            errorResponse.Error.Message.ShouldBe(Error.Message[Error.NotFound]);
        }

        [Fact]
        public async Task Query_NonExist_Api_Failed_Test()
        {
            var getResponse = await GetResponseAsObjectAsync<WebAppErrorResponse>("/api/blockChain/TestMethod",
                expectedStatusCode: HttpStatusCode.NotFound);
            getResponse.ShouldBeNull();
            var postResponse = await PostResponseAsObjectAsync<WebAppErrorResponse>("/api/blockChain/TestMethod",
                new Dictionary<string, string>(),
                expectedStatusCode: HttpStatusCode.NotFound);
            postResponse.ShouldBeNull();
            var deleteResponse = await DeleteResponseAsObjectAsync<WebAppErrorResponse>("/api/blockChain/TestMethod",
                expectedStatusCode: HttpStatusCode.NotFound);
            deleteResponse.ShouldBeNull();
        }

        [Fact]
        public async Task Get_ContractFileDescriptorSet_Success_Test()
        {
            // Generate a transaction and broadcast
            var transaction = await _osTestHelper.GenerateTransferTransaction();
            await _osTestHelper.BroadcastTransactions(new List<Transaction> {transaction});

            await _osTestHelper.MinedOneBlock();

            var response = await GetResponseAsStringAsync(
                $"/api/blockChain/contractFileDescriptorSet?address={transaction.To.ToBase58()}");
            response.ShouldNotBeEmpty();
            var set = FileDescriptorSet.Parser.ParseFrom(
                ByteString.FromBase64(response.Substring(1, response.Length - 2)));
            set.ShouldNotBeNull();
            set.File.Count.ShouldBeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task Get_ContractFileDescriptorSet_Failed_Test()
        {
            var addressInfo = SampleAddress.AddressList[0].ToBase58();
            var response = await GetResponseAsObjectAsync<WebAppErrorResponse>(
                $"/api/blockChain/contractFileDescriptorSet?address={addressInfo}",
                expectedStatusCode: HttpStatusCode.Forbidden);
            response.Error.Code.ShouldBe(Error.NotFound.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.NotFound]);

            addressInfo = "invalid address";
            var errorResponse = await GetResponseAsObjectAsync<WebAppErrorResponse>(
                $"/api/blockChain/contractFileDescriptorSet?address={addressInfo}",
                expectedStatusCode: HttpStatusCode.Forbidden);
            errorResponse.Error.Code.ShouldBe(Error.NotFound.ToString());
            errorResponse.Error.Message.ShouldBe(Error.Message[Error.NotFound]);
        }

        [Fact]
        public async Task CreateRawTransaction_Failed_Test()
        {
            var newUserKeyPair = CryptoHelper.GenerateKeyPair();
            var toAddressValue =
                Base64.ToBase64String(Address.FromPublicKey(newUserKeyPair.PublicKey).Value.ToByteArray());
            var parameters = new Dictionary<string, string>
            {
                {"From", "6WZNJgU5MHWsvzZmPpC7cW6g3qciniQhDKRLCvbQcTCcVF1"},
                {"To", "4rkKQpsRFt1nU6weAHuJ6CfQDqo6dxruU3K3wNUFr6ZwZY1"},
                {"RefBlockNumber", "2788"},
                {"RefBlockHash", "190db8baaad13e40900a6a5970915a1402d18f2b685e2183efdd954ba890a4182"},
                {"MethodName", "Transfer"},
                {
                    "Params",
                    "{\"to\":{ \"Value\": \"" + toAddressValue +
                    "\" },\"symbol\":\"ELF\",\"amount\":100,\"memo\":\"test\"}"
                }
            };
            var response =
                await PostResponseAsObjectAsync<WebAppErrorResponse>(
                    "/api/blockChain/rawTransaction", parameters, expectedStatusCode: HttpStatusCode.BadRequest);
            response.Error.ValidationErrors.Any(v => v.Message == Error.Message[Error.InvalidAddress]).ShouldBeTrue();
            response.Error.ValidationErrors.Any(v => v.Message == Error.Message[Error.InvalidBlockHash]).ShouldBeTrue();
            response.Error.ValidationErrors.First(v => v.Message == Error.Message[Error.InvalidBlockHash]).Members
                .ShouldContain("refBlockHash");
            response.Error.ValidationErrors.Any(v => v.Members.Contains("to")).ShouldBeTrue();
            response.Error.ValidationErrors.Any(v => v.Members.Contains("from")).ShouldBeTrue();

            var contractAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(await _osTestHelper.GetChainContextAsync(), TokenSmartContractAddressNameProvider.StringName);
            var accountAddress = await _accountService.GetAccountAsync();
            parameters = new Dictionary<string, string>
            {
                {"From", contractAddress.ToBase58()},
                {"To", accountAddress.ToBase58()},
                {"RefBlockNumber", "2788"},
                {"RefBlockHash", "190db8baaad13e40900a6a5970915a1402d18f2b685e2183efdd954ba890a418"},
                {"MethodName", "Transfer"},
                {
                    "Params",
                    "{\"to\":{ \"Value\": \"" + toAddressValue +
                    "\" },\"symbol\":\"ELF\",\"amount\":100,\"memo\":\"test\"}"
                }
            };
            response =
                await PostResponseAsObjectAsync<WebAppErrorResponse>(
                    "/api/blockChain/rawTransaction", parameters, expectedStatusCode: HttpStatusCode.Forbidden);
            response.Error.Code.ShouldBe(Error.InvalidContractAddress.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.InvalidContractAddress]);

            parameters = new Dictionary<string, string>
            {
                {"From", accountAddress.ToBase58()},
                {"To", contractAddress.ToBase58()},
                {"RefBlockNumber", "2788"},
                {"RefBlockHash", "190db8baaad13e40900a6a5970915a1402d18f2b685e2183efdd954ba890a418"},
                {"MethodName", "NoMethod"},
                {
                    "Params",
                    "{\"to\":{ \"Value\": \"" + toAddressValue +
                    "\" },\"symbol\":\"ELF\",\"amount\":100,\"memo\":\"test\"}"
                }
            };
            response =
                await PostResponseAsObjectAsync<WebAppErrorResponse>(
                    "/api/blockChain/rawTransaction", parameters, expectedStatusCode: HttpStatusCode.Forbidden);
            response.Error.Code.ShouldBe(Error.NoMatchMethodInContractAddress.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.NoMatchMethodInContractAddress]);

            parameters = new Dictionary<string, string>
            {
                {"From", accountAddress.ToBase58()},
                {"To", contractAddress.ToBase58()},
                {"RefBlockNumber", "2788"},
                {"RefBlockHash", "190db8baaad13e40900a6a5970915a1402d18f2b685e2183efdd954ba890a418"},
                {"MethodName", "Transfer"},
                {"Params", "NoParams"}
            };
            response =
                await PostResponseAsObjectAsync<WebAppErrorResponse>(
                    "/api/blockChain/rawTransaction", parameters, expectedStatusCode: HttpStatusCode.Forbidden);
            response.Error.Code.ShouldBe(Error.InvalidParams.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.InvalidParams]);

            parameters = new Dictionary<string, string>
            {
                {"From", accountAddress.ToBase58()},
                {"To", contractAddress.ToBase58()},
                {"RefBlockNumber", "2788"},
                {"RefBlockHash", "190db8baaad13e40900a6a5970915a1402d18f2b685e2183efdd954ba890a418"},
                {"MethodName", "Transfer"},
                {
                    "Params",
                    "{\"to\":{ \"Value\": \"" + Base64.ToBase64String(Encoding.UTF8.GetBytes("InvalidAddress")) +
                    "\" },\"symbol\":\"ELF\",\"amount\":100,\"memo\":\"test\"}"
                }
            };
            response =
                await PostResponseAsObjectAsync<WebAppErrorResponse>(
                    "/api/blockChain/rawTransaction", parameters, expectedStatusCode: HttpStatusCode.Forbidden);
            response.Error.Code.ShouldBe(Error.InvalidParams.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.InvalidParams]);
        }

        [Fact]
        public async Task CreateRawTransaction_Success_Test()
        {
            var transferToAddress =
                Address.FromBase58("21oXyCxvUd7YUUkgbZxkbmu4EWs65yos6iVC39rPwPknune6qZ");
            var toAddress = Base64.ToBase64String(transferToAddress.Value.ToByteArray());
            var contractAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(await _osTestHelper.GetChainContextAsync(), TokenSmartContractAddressNameProvider.StringName);
            OutputHelper.WriteLine(contractAddress.ToString()); //2J9wWhuyz7Drkmtu9DTegM9rLmamjekmRkCAWz5YYPjm7akfbH
            var fromAddressInBase58 = "juYfvugva4PZSEz1w9J8VkAhgrbevEmqTLSATwc9i1XHZJvE1";
            var refHashInHex = "190db8baaad13e40900a6a5970915a1402d18f2b685e2183efdd954ba890a418"; 
            var parameters = new Dictionary<string, string>
            {
                {"From", fromAddressInBase58},
                {"To", contractAddress.ToBase58()},
                {"RefBlockNumber", "2788"},
                {"RefBlockHash", refHashInHex},
                {"MethodName", "Transfer"},
                {
                    "Params",
                    "{\"to\":{ \"value\": \"" + toAddress + "\" },\"symbol\":\"ELF\",\"amount\":100,\"memo\":\"test\"}"
                }
            };
            var response =
                await PostResponseAsObjectAsync<CreateRawTransactionOutput>("/api/blockChain/rawTransaction",
                    parameters);
            var tx = Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(response.RawTransaction));
            tx.From.ShouldBe(Address.FromBase58(fromAddressInBase58));
            tx.To.ShouldBe(contractAddress);
            tx.RefBlockNumber.ShouldBe(2788);
            tx.RefBlockPrefix.ShouldBe(BlockHelper.GetRefBlockPrefix(Hash.LoadFromHex(refHashInHex)));
            tx.MethodName.ShouldBe("Transfer");
            var transferInput = TransferInput.Parser.ParseFrom(tx.Params);
            transferInput.Amount.ShouldBe(100);
            transferInput.Memo.ShouldBe("test");
            transferInput.Symbol.ShouldBe("ELF");
            transferInput.To.ShouldBe(transferToAddress);
        }

        [Fact]
        public async Task SendRawTransaction_Success_Test()
        {
            const string methodName = "Transfer";
            var contractAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(await _osTestHelper.GetChainContextAsync(), TokenSmartContractAddressNameProvider.StringName);
            var chain = await _blockchainService.GetChainAsync();
            var newUserKeyPair = CryptoHelper.GenerateKeyPair();
            var accountAddress = await _accountService.GetAccountAsync();
            var toAddress = Base64.ToBase64String(Address.FromPublicKey(newUserKeyPair.PublicKey).Value.ToByteArray());
            var parameters = new Dictionary<string, string>
            {
                {"From", accountAddress.ToBase58()},
                {"To", contractAddress.ToBase58()},
                {"RefBlockNumber", chain.BestChainHeight.ToString()},
                {"RefBlockHash", chain.BestChainHash.ToHex()},
                {"MethodName", methodName},
                {
                    "Params",
                    "{\"to\":{ \"value\": \"" + toAddress + "\" },\"symbol\":\"ELF\",\"amount\":100,\"memo\":\"test\"}"
                }
            };
            var createTransactionResponse =
                await PostResponseAsObjectAsync<CreateRawTransactionOutput>("/api/blockChain/rawTransaction",
                    parameters);
            var transactionId =
                HashHelper.ComputeFrom(ByteArrayHelper.HexStringToByteArray(createTransactionResponse.RawTransaction));

            var signature = await _accountService.SignAsync(transactionId.ToByteArray());
            parameters = new Dictionary<string, string>
            {
                {"Transaction", createTransactionResponse.RawTransaction},
                {"Signature", signature.ToHex()}
            };
            var sendTransactionResponse =
                await PostResponseAsObjectAsync<SendRawTransactionOutput>("/api/blockChain/sendRawTransaction",
                    parameters);

            sendTransactionResponse.TransactionId.ShouldBe(transactionId.ToHex());
            sendTransactionResponse.Transaction.ShouldBeNull();

            var existTransaction = await _txHub.GetExecutableTransactionSetAsync();
            existTransaction.Transactions[0].GetHash().ToHex().ShouldBe(sendTransactionResponse.TransactionId);

            parameters = new Dictionary<string, string>
            {
                {"Transaction", createTransactionResponse.RawTransaction},
                {"Signature", signature.ToHex()},
                {"returnTransaction", "true"}
            };
            sendTransactionResponse =
                await PostResponseAsObjectAsync<SendRawTransactionOutput>("/api/blockChain/sendRawTransaction",
                    parameters);

            sendTransactionResponse.TransactionId.ShouldBe(transactionId.ToHex());
            sendTransactionResponse.Transaction.ShouldNotBeNull();
            sendTransactionResponse.Transaction.To.ShouldBe(contractAddress.ToBase58());
            sendTransactionResponse.Transaction.From.ShouldBe(accountAddress.ToBase58());
            sendTransactionResponse.Transaction.MethodName.ShouldBe(methodName);
            sendTransactionResponse.Transaction.Params.ShouldBe(
                "{ \"to\": \"" + Address.FromPublicKey(newUserKeyPair.PublicKey).ToBase58() +
                "\", \"symbol\": \"ELF\", \"amount\": \"100\", \"memo\": \"test\" }");
            sendTransactionResponse.Transaction.RefBlockNumber.ShouldBe(chain.BestChainHeight);
            sendTransactionResponse.Transaction.RefBlockPrefix.ShouldBe(BlockHelper.GetRefBlockPrefix(chain.BestChainHash).ToBase64());
            sendTransactionResponse.Transaction.Signature.ShouldBe(ByteString.CopyFrom(signature).ToBase64());

            existTransaction = await _txHub.GetExecutableTransactionSetAsync();
            existTransaction.Transactions[0].GetHash().ToHex().ShouldBe(sendTransactionResponse.TransactionId);
        }

        [Fact]
        public async Task SendRawTransaction_ReturnInvalidTransaction_Test()
        {
            var contractAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(await _osTestHelper.GetChainContextAsync(), TokenSmartContractAddressNameProvider.StringName);

            var from = Base64.ToBase64String(Encoding.UTF8.GetBytes("InvalidAddress"));
            var to = Base64.ToBase64String(contractAddress.Value.ToByteArray());
            var json =
                "{ \"from\": { \"value\": \"" + from + "\" }, \"to\": { \"value\": \"" + to +
                "\" }, \"ref_block_number\": \"11\", \"ref_block_prefix\": \"H9f1zQ==\", \"method_name\": \"Transfer\", \"params\": \"CiIKIDAK0LTy1ZAHaf1nAnq/gkSqTCs4Kh6czxWpbNEX4EwaEgNFTEYYFA==\"}";
            var transaction = Transaction.Parser.ParseJson(json);

            var signature = await _accountService.SignAsync(transaction.GetHash().ToByteArray());
            var parameters = new Dictionary<string, string>
            {
                {"Transaction", transaction.ToByteArray().ToHex()},
                {"Signature", signature.ToHex()}
            };
            var errorResponse =
                await PostResponseAsObjectAsync<WebAppErrorResponse>("/api/blockChain/sendRawTransaction",
                    parameters, expectedStatusCode: HttpStatusCode.Forbidden);
            errorResponse.Error.Code.ShouldBe(Error.InvalidTransaction.ToString());
            errorResponse.Error.Message.ShouldBe(Error.Message[Error.InvalidTransaction]);
        }

        [Fact]
        public async Task SendRawTransaction_ReturnNoMatchMethodInContractAddress_Test()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var from = Base64.ToBase64String(accountAddress.Value.ToByteArray());
            var contractAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(await _osTestHelper.GetChainContextAsync(), TokenSmartContractAddressNameProvider.StringName);
            var to = Base64.ToBase64String(contractAddress.Value.ToByteArray());
            var json =
                "{ \"from\": { \"value\": \"" + from + "\" }, \"to\": { \"value\": \"" + to +
                "\" }, \"ref_block_number\": \"11\", \"ref_block_prefix\": \"H9f1zQ==\", \"method_name\": \"invalid_method\", \"params\": \"CiIKIDAK0LTy1ZAHaf1nAnq/gkSqTCs4Kh6czxWpbNEX4EwaEgNFTEYYFA==\"}";
            var transaction = Transaction.Parser.ParseJson(json);

            var signature = await _accountService.SignAsync(transaction.GetHash().ToByteArray());
            var parameters = new Dictionary<string, string>
            {
                {"Transaction", transaction.ToByteArray().ToHex()},
                {"Signature", signature.ToHex()}
            };
            var errorResponse =
                await PostResponseAsObjectAsync<WebAppErrorResponse>("/api/blockChain/sendRawTransaction",
                    parameters, expectedStatusCode: HttpStatusCode.Forbidden);
            errorResponse.Error.Code.ShouldBe(Error.NoMatchMethodInContractAddress.ToString());
            errorResponse.Error.Message.ShouldBe(Error.Message[Error.NoMatchMethodInContractAddress]);
        }

        [Fact]
        public async Task SendRawTransaction_ReturnInvalidParams_Test()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var from = Base64.ToBase64String(accountAddress.Value.ToByteArray());
            var contractAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(await _osTestHelper.GetChainContextAsync(), TokenSmartContractAddressNameProvider.StringName);
            var to = Base64.ToBase64String(contractAddress.Value.ToByteArray());
            var transactionParams = TransferInput.Parser.ParseJson(
                "{\"to\":{ \"value\": \"" + Base64.ToBase64String(Encoding.UTF8.GetBytes("InvalidAddress")) +
                "\" },\"symbol\":\"ELF\",\"amount\":100,\"memo\":\"test\"}");

            var json = "{ \"from\": { \"value\": \"" + from + "\" }, \"to\": { \"value\": \"" + to +
                       "\" }, \"ref_block_number\": \"11\", \"ref_block_prefix\": \"H9f1zQ==\", \"method_name\": \"Transfer\", \"params\": \"" +
                       Base64.ToBase64String(transactionParams.ToByteArray()) + "\" }";
            var transaction = Transaction.Parser.ParseJson(json);

            var signature = await _accountService.SignAsync(transaction.GetHash().ToByteArray());
            var parameters = new Dictionary<string, string>
            {
                {"Transaction", transaction.ToByteArray().ToHex()},
                {"Signature", signature.ToHex()}
            };
            var errorResponse =
                await PostResponseAsObjectAsync<WebAppErrorResponse>("/api/blockChain/sendRawTransaction",
                    parameters, expectedStatusCode: HttpStatusCode.Forbidden);
            errorResponse.Error.Code.ShouldBe(Error.InvalidParams.ToString());
            errorResponse.Error.Message.ShouldBe(Error.Message[Error.InvalidParams]);

            var issueInput = IssueInput.Parser.ParseJson(
                "{\"to\":{ \"value\": \"" + Base64.ToBase64String(Encoding.UTF8.GetBytes("InvalidHash")) +
                "\" },\"symbol\":\"ELF\",\"amount\":100,\"memo\":\"test\"}");

            json = "{ \"from\": { \"value\": \"" + from + "\" }, \"to\": { \"value\": \"" + to +
                   "\" }, \"ref_block_number\": \"11\", \"ref_block_prefix\": \"H9f1zQ==\", \"method_name\": \"Issue\", \"params\": \"" +
                   Base64.ToBase64String(issueInput.ToByteArray()) + "\" }";
            transaction = Transaction.Parser.ParseJson(json);

            signature = await _accountService.SignAsync(transaction.GetHash().ToByteArray());
            parameters = new Dictionary<string, string>
            {
                {"Transaction", transaction.ToByteArray().ToHex()},
                {"Signature", signature.ToHex()}
            };
            errorResponse =
                await PostResponseAsObjectAsync<WebAppErrorResponse>("/api/blockChain/sendRawTransaction",
                    parameters, expectedStatusCode: HttpStatusCode.Forbidden);
            errorResponse.Error.Code.ShouldBe(Error.InvalidParams.ToString());
            errorResponse.Error.Message.ShouldBe(Error.Message[Error.InvalidParams]);
        }

        [Fact]
        public async Task GetTaskQueueStateInfos_Test()
        {
            var response =
                await GetResponseAsObjectAsync<List<TaskQueueInfoDto>>("/api/blockChain/taskQueueStatus");

            var count = response.Count;
            count.ShouldBeGreaterThan(0);
        }

        [Fact]
        public async Task GetMerklePathByTransactionId_Success_Test()
        {
            var transactionList = new List<Transaction>();
            for (var i = 0; i < 3; i++)
            {
                var transaction = await _osTestHelper.GenerateTransferTransaction();
                transactionList.Add(transaction);
            }

            await _osTestHelper.BroadcastTransactions(transactionList);
            var block = await _osTestHelper.MinedOneBlock();
            // After mined
            var merkleTreeRoot = block.Header.MerkleTreeRootOfTransactionStatus;
            var txHex = block.Body.TransactionIds[0].ToHex();

            var response = await GetResponseAsObjectAsync<MerklePathDto>(
                $"/api/blockChain/merklePathByTransactionId?transactionId={txHex}");
            var merklePath = new MerklePath();
            foreach (var res in response.MerklePathNodes)
            {
                merklePath.MerklePathNodes.Add(new MerklePathNode
                {
                    Hash = Hash.LoadFromHex(res.Hash), IsLeftChildNode = res.IsLeftChildNode
                });
            }

            var transactionResult = await _osTestHelper.GetTransactionResultsAsync(block.Body.TransactionIds[0]);
            var calculatedRoot = merklePath.ComputeRootWithLeafNode(
                GetHashCombiningTransactionAndStatus(transactionResult.TransactionId, transactionResult.Status));

            var merklePathNodes = response.MerklePathNodes;
            merklePathNodes.Count.ShouldBe(2);

            Assert.True(merklePathNodes[0].IsLeftChildNode == false);
            Assert.True(merklePathNodes[1].IsLeftChildNode == false);
            Assert.True(merkleTreeRoot == calculatedRoot);
        }

        [Fact]
        public async Task GetMerklePathByTransactionId_Failed_Test()
        {
            string hex = "5a7d71da020cae179a0dfe82bd3c967e1573377578f4cc87bc21f74f2556c0ef";

            var errorResponse = await GetResponseAsObjectAsync<WebAppErrorResponse>(
                $"/api/blockChain/merklePathByTransactionId?transactionId={hex}",
                expectedStatusCode: HttpStatusCode.Forbidden);
            errorResponse.Error.Code.ShouldBe(Error.NotFound.ToString());
            errorResponse.Error.Message.ShouldBe(Error.Message[Error.NotFound]);
        }

        private async Task<List<Transaction>> GenerateTwoInitializeTransaction()
        {
            var transactionList = new List<Transaction>();
            var newUserKeyPair = CryptoHelper.GenerateKeyPair();

            for (int i = 0; i < 2; i++)
            {
                var transaction = _osTestHelper.GenerateTransaction(Address.FromPublicKey(newUserKeyPair.PublicKey),
                    await _smartContractAddressService.GetAddressByContractNameAsync(await _osTestHelper.GetChainContextAsync(), TokenSmartContractAddressNameProvider.StringName),
                    nameof(TokenContractContainer.TokenContractStub.Create), new CreateInput
                    {
                        Symbol = "ELF",
                        TokenName = $"elf token {i}",
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

        private async Task<Transaction> GenerateViewTransaction(string method, IMessage input)
        {
            var newUserKeyPair = CryptoHelper.GenerateKeyPair();

            var transaction = _osTestHelper.GenerateTransaction(Address.FromPublicKey(newUserKeyPair.PublicKey),
                await _smartContractAddressService.GetAddressByContractNameAsync(await _osTestHelper.GetChainContextAsync(), TokenSmartContractAddressNameProvider.StringName),
                method, input);

            var signature = CryptoHelper.SignWithPrivateKey(newUserKeyPair.PrivateKey,
                transaction.GetHash().ToByteArray());
            transaction.Signature = ByteString.CopyFrom(signature);

            return transaction;
        }
        
        private Hash GetHashCombiningTransactionAndStatus(Hash txId,
            TransactionResultStatus executionReturnStatus)
        {
            // combine tx result status
            var rawBytes = txId.ToByteArray().Concat(Encoding.UTF8.GetBytes(executionReturnStatus.ToString()))
                .ToArray();
            return HashHelper.ComputeFrom(rawBytes);
        }
    }
}