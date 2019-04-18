using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.Token;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS;
using AElf.Runtime.CSharp;
using AElf.WebApp.Application.Chain.Dto;
using Google.Protobuf;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElf.WebApp.Application.Chain.Tests
{
    public sealed class BlockChainAppServiceTest : WebAppTestBase
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITxHub _txHub;
        private readonly IBlockchainStateMergingService _blockchainStateMergingService;
        private readonly IBlockchainStateManager _blockchainStateManager;
        private readonly OSTestHelper _osTestHelper;

        public BlockChainAppServiceTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _txHub = GetRequiredService<ITxHub>();
            _blockchainStateMergingService = GetRequiredService<IBlockchainStateMergingService>();
            _blockchainStateManager = GetRequiredService<IBlockchainStateManager>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
        }
        
        [Fact]
        public async Task GetBlockHeightTest()
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

            var response = await PostResponseAsStringAsync("/api/blockChain/call",paramters);
            response.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async Task Call_Failed()
        {
            var paramters = new Dictionary<string, string>
            {
                {"rawTransaction", "0a200a1e4604ccbdaa377fd7022b56436b99309e8b71cc5d78e909d271dbd1aeee6412200a1eaaa58b6cf58d4ef337f6dc55b701fd57d622015a3548a91a4e40892aa355180b220436957f93320c476574546f6b656e496e666f3a060a04454c46324a416246d781d80759d8ae6bb895b17203a3c9d4e89f083d7d89d9b6cbbf1c67ded52e134108fc8b3646f6549313868ce3e68a7117815cc0c2107ef1a986430a12ba002"}
            };
            var response = await PostResponseAsObjectAsync<WebAppErrorResponse>("/api/blockChain/call",paramters,expectedStatusCode: HttpStatusCode.Forbidden);
            
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
                await PostResponseAsObjectAsync<BroadcastTransactionOutput>("/api/blockChain/broadcastTransaction", parameters);

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
                await PostResponseAsObjectAsync<WebAppErrorResponse>("/api/blockChain/broadcastTransaction",
                    parameters, expectedStatusCode: HttpStatusCode.Forbidden);

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
                "/api/blockChain/broadcastTransaction", parameters, expectedStatusCode: HttpStatusCode.Forbidden);

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
                await PostResponseAsObjectAsync<string[]>("/api/blockChain/broadcastTransactions", parameters);
            var responseTransactionIds = broadcastTransactionsResponse.ToList();

            responseTransactionIds.Count.ShouldBe(2);

            var existTransaction = await _txHub.GetExecutableTransactionSetAsync();
            existTransaction.Transactions.Select(x=>x.GetHash().ToHex()).ShouldContain(responseTransactionIds[0]);
            existTransaction.Transactions.Select(x=>x.GetHash().ToHex()).ShouldContain(responseTransactionIds[1]);

            var getTransactionPoolStatusResponse = await GetResponseAsObjectAsync<GetTransactionPoolStatusOutput>("/api/blockChain/transactionPoolStatus");
            getTransactionPoolStatusResponse.Queued.ShouldBe(2);
        }
        
        [Fact]
        public async Task Get_TransactionResult_Success()
        {
            // Generate a transaction and broadcast
            var transaction = await _osTestHelper.GenerateTransferTransaction();
            var transactionHex = transaction.GetHash().ToHex();
            await _osTestHelper.BroadcastTransactions(new List<Transaction> {transaction});

            // Before mined
            var response = await GetResponseAsObjectAsync<TransactionResultDto>(
                $"/api/blockChain/transactionResult?transactionId={transactionHex}");

            response.TransactionId.ShouldBe(transactionHex);
            response.Status.ShouldBe(TransactionResultStatus.Pending.ToString());

            await _osTestHelper.MinedOneBlock();

            // After mined
            response = await GetResponseAsObjectAsync<TransactionResultDto>(
                $"/api/blockChain/transactionResult?transactionId={transactionHex}");

            response.TransactionId.ShouldBe(transactionHex);
            response.Status.ShouldBe(TransactionResultStatus.Mined.ToString());
        }
        
        [Fact]
        public async Task Get_Failed_TransactionResult_Success()
        {
            // Generate a transaction and broadcast
            var transactionList = await GenerateTwoInitializeTransaction();
            await _osTestHelper.BroadcastTransactions(transactionList);
            
            await _osTestHelper.MinedOneBlock();

            // After executed
            var transactionHex = transactionList[1].GetHash().ToHex();
            var response = await GetResponseAsObjectAsync<TransactionResultDto>(
                $"/api/blockChain/transactionResult?transactionId={transactionHex}");

            response.TransactionId.ShouldBe(transactionHex);
            response.Status.ShouldBe(TransactionResultStatus.Failed.ToString());
            response.Error.Contains("Token already exists.").ShouldBeTrue();
        }
        
        [Fact]
        public async Task Get_NotExisted_TransactionResult()
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
        public async Task Get_TransactionResult_ReturnInvalidTransactionId()
        {
            var fakeTransactionId = "FakeTransactionId";
            var response = await GetResponseAsObjectAsync<WebAppErrorResponse>(
                $"/api/blockChain/transactionResult?transactionId={fakeTransactionId}", expectedStatusCode: HttpStatusCode.Forbidden);           

            response.Error.Code.ShouldBe(Error.InvalidTransactionId.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.InvalidTransactionId]);
        }
        
        [Fact]
        public async Task Get_TransactionResults_Success()
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

            response = await GetResponseAsObjectAsync<List<TransactionResultDto>>(
                $"/api/blockChain/transactionResults?blockHash={block.GetHash().ToHex()}&offset=15&limit=15");
   
            response.Count.ShouldBe(5);
        }
        
        [Fact]
        public async Task Get_NotExisted_TransactionResults()
        {
            var block = new Block
            {
                Header = new BlockHeader(),
            };
            var blockHash = block.GetHash().ToHex();
            var response = await GetResponseAsObjectAsync<WebAppErrorResponse>(
                $"/api/blockChain/transactionResults?blockHash={blockHash}&offset=0&limit=10",
                expectedStatusCode: HttpStatusCode.Forbidden);

            response.Error.Code.ShouldBe(Error.NotFound.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.NotFound]);
        }
        
        [Fact]
        public async Task Get_TransactionResults_With_InvalidParameter()
        {
            var block = new Block
            {
                Header = new BlockHeader(),
            };
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
        }
        
        [Fact]
        public async Task Get_Block_By_BlockHeight_Success()
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
            response.Header.ChainId.ShouldBe(ChainHelpers.ConvertChainIdToBase58(chain.Id));
            response.Header.Bloom.ShouldBe(block.Header.Bloom.ToByteArray().ToHex());
            response.Body.TransactionsCount.ShouldBe(3);

            var responseTransactions = response.Body.Transactions;
            responseTransactions.Count.ShouldBe(3);
        }
        
        [Fact]
        public async Task Get_Block_By_BlockHash_Success()
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
            response.Header.ChainId.ShouldBe(ChainHelpers.ConvertChainIdToBase58(chain.Id));
            response.Header.Bloom.ShouldBe(block.Header.Bloom.ToByteArray().ToHex());
            response.Body.TransactionsCount.ShouldBe(3);

            var responseTransactions = response.Body.Transactions;
            responseTransactions.Count.ShouldBe(3);
        }
        
        [Fact]
        public async Task Get_Block_ReturnNotFound()
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
        }

        [Fact]
        public async Task Get_Chain_Status_Success()
        {
            var chain = await _blockchainService.GetChainAsync();
            var basicContractZero = _smartContractAddressService.GetZeroSmartContractAddress();
            
            var response = await GetResponseAsObjectAsync<ChainStatusDto>("/api/blockChain/chainStatus");
            response.Branches.ShouldNotBeNull();
            var responseChainId = ChainHelpers.ConvertBase58ToChainId(response.ChainId);
            responseChainId.ShouldBe(chain.Id);
            response.GenesisContractAddress.ShouldBe(basicContractZero.GetFormatted());
            response.BestChainHeight.ShouldBe(11);
            response.BestChainHash.ShouldBe(chain.BestChainHash?.ToHex());
            response.LongestChainHeight = chain.LongestChainHeight;
            response.LongestChainHash = chain.LongestChainHash?.ToHex();
            response.GenesisBlockHash = chain.GenesisBlockHash.ToHex();
            response.LastIrreversibleBlockHash = chain.LastIrreversibleBlockHash?.ToHex();
            response.LastIrreversibleBlockHeight = chain.LastIrreversibleBlockHeight;
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
        public async Task Get_Block_State_FewBlocks_Later()
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

            var blockStateSet = await _blockchainStateManager.GetBlockStateSetAsync(block.GetHash());
            await _blockchainStateMergingService.MergeBlockStateAsync(blockStateSet.BlockHeight,
                blockStateSet.BlockHash);

            var errorResponse = await GetResponseAsObjectAsync<WebAppErrorResponse>(
                $"/api/blockChain/blockState?blockHash={block.GetHash().ToHex()}",
                expectedStatusCode: HttpStatusCode.Forbidden);
            errorResponse.Error.Code.ShouldBe(Error.NotFound.ToString());
            errorResponse.Error.Message.ShouldBe(Error.Message[Error.NotFound]);
        }
        
        [Fact]
        public async Task Query_NonExist_Api_Failed()
        {
            var getResponse = await GetResponseAsObjectAsync<WebAppErrorResponse>("/api/blockChain/TestMethod",
                expectedStatusCode: HttpStatusCode.NotFound);
            getResponse.ShouldBeNull();
            var postResponse = await PostResponseAsObjectAsync<WebAppErrorResponse>("/api/blockChain/TestMethod",new Dictionary<string, string>(),
                expectedStatusCode: HttpStatusCode.NotFound);
            postResponse.ShouldBeNull();
            var deleteResponse = await DeleteResponseAsObjectAsync<WebAppErrorResponse>("/api/blockChain/TestMethod",
                expectedStatusCode: HttpStatusCode.NotFound);
            deleteResponse.ShouldBeNull();
        }
        
        [Fact]
        public async Task Get_FileDescriptorSet_Success()
        {
            // Generate a transaction and broadcast
            var transaction = await _osTestHelper.GenerateTransferTransaction();
            await _osTestHelper.BroadcastTransactions(new List<Transaction> {transaction});
            
            await _osTestHelper.MinedOneBlock();
            
            var response = await GetResponseAsStringAsync(
                $"/api/blockChain/fileDescriptorSet?address={transaction.To.GetFormatted()}");
            response.ShouldNotBeEmpty();
            var set = FileDescriptorSet.Parser.ParseFrom(ByteString.FromBase64(response.Substring(1,response.Length-2)));
            set.ShouldNotBeNull();
            set.File.Count.ShouldBeGreaterThanOrEqualTo(1);
        }
        
        [Fact]
        public async Task Get_FileDescriptorSet_Failed()
        {
            var addressInfo = Address.Generate().GetFormatted();
            var response = await GetResponseAsObjectAsync<WebAppErrorResponse>(
                $"/api/blockChain/fileDescriptorSet?address={addressInfo}", expectedStatusCode: HttpStatusCode.Forbidden);
            response.Error.Code.ShouldBe(Error.NotFound.ToString());
            response.Error.Message.ShouldBe(Error.Message[Error.NotFound]);

            addressInfo = "invalid address";
            var response1 = await GetResponseAsObjectAsync<WebAppErrorResponse>(
                $"/api/blockChain/fileDescriptorSet?address={addressInfo}", expectedStatusCode: HttpStatusCode.Forbidden);
            response.Error.Code.ShouldBe(Error.NotFound.ToString());
            response1.Error.Message.ShouldBe(Error.Message[Error.NotFound]);
        }
        
        private Task<List<Transaction>> GenerateTwoInitializeTransaction()
        {
            var transactionList = new List<Transaction>();
            var newUserKeyPair = CryptoHelpers.GenerateKeyPair();

            for (int i = 0; i < 2; i++)
            {
                var transaction = _osTestHelper.GenerateTransaction(Address.FromPublicKey(newUserKeyPair.PublicKey),
                    _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name),
                    nameof(TokenContract.Create), new CreateInput
                    {
                        Symbol = $"ELF",
                        TokenName= $"elf token {i}",
                        TotalSupply = 1000_0000,
                        Decimals = 2,
                        Issuer = Address.Generate(),
                        IsBurnable = true
                    });

                var signature =
                    CryptoHelpers.SignWithPrivateKey(newUserKeyPair.PrivateKey, transaction.GetHash().DumpByteArray());
                transaction.Sigs.Add(ByteString.CopyFrom(signature));

                transactionList.Add(transaction); 
            }
            
            return Task.FromResult(transactionList);
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