using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Genesis;
using AElf.Contracts.Token;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types.CSharp;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;
using Xunit.Abstractions;

namespace AElf.OS.Rpc.ChainController.Tests
{
    public class ChainControllerRpcServiceServerTest : RpcTestBase
    {
        public ILogger<ChainControllerRpcServiceServerTest> Logger { get; set; }
        private readonly IBlockchainService _blockchainService;
        private readonly IMinerService _minerService;
        private readonly ITxHub _txHub;
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;
        private readonly IAccountService _accountService;
        private readonly ECKeyPair _userEcKeyPair;

        public ChainControllerRpcServiceServerTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            Logger = GetService<ILogger<ChainControllerRpcServiceServerTest>>() ??
                     NullLogger<ChainControllerRpcServiceServerTest>.Instance;

            _blockchainService = GetRequiredService<IBlockchainService>();
            _txHub = GetRequiredService<ITxHub>();
            _minerService = GetRequiredService<IMinerService>();
            _smartContractExecutiveService = GetRequiredService<ISmartContractExecutiveService>();
            _accountService = GetRequiredService<IAccountService>();

            _userEcKeyPair = CryptoHelpers.GenerateKeyPair();

            AsyncHelper.RunSync(async () => await InitAccountAmount());
        }

        [Fact]
        public async Task Get_BlockHeight_Success()
        {
            // Get current height
            var response = await JsonCallAsJObject("/chain", "GetBlockHeight");
            var currentHeight = (int) response["result"];

            // Mined one block
            var chain = await _blockchainService.GetChainAsync();
            var transaction = await GenerateTransferTransaction(chain);
            await BroadcastTransactions(new List<Transaction> {transaction});
            await MinedOneBlock(chain);

            // Get latest height
            response = await JsonCallAsJObject("/chain", "GetBlockHeight");
            var height = (int) response["result"];
            height.ShouldBe(currentHeight + 1);
        }

        [Fact]
        public async Task Connect_Chain_Success()
        {
            var chainId = _blockchainService.GetChainId();
            var basicContractZero = Address.BuildContractAddress(chainId, 0);

            var response = await JsonCallAsJObject("/chain", "ConnectChain");

            var responseZeroContractAddress =
                response["result"][SmartContract.GenesisSmartContractZeroAssemblyName].ToString();
            var responseChainId = ChainHelpers.ConvertBase58ToChainId(response["result"]["ChainId"].ToString());

            responseZeroContractAddress.ShouldBe(basicContractZero.GetFormatted());
            responseChainId.ShouldBe(chainId);
        }

        [Fact]
        public async Task Get_ContractAbi_Success()
        {
            // Deploy a new contact and mined
            var chain = await _blockchainService.GetChainAsync();
            var transaction = await GenerateTransaction(chain, Address.FromPublicKey(_userEcKeyPair.PublicKey),
                Address.BuildContractAddress(chain.Id, 0), nameof(ISmartContractZero.DeploySmartContract), 2,
                File.ReadAllBytes(typeof(BasicContractZero).Assembly.Location));
            var signature =
                CryptoHelpers.SignWithPrivateKey(_userEcKeyPair.PrivateKey, transaction.GetHash().DumpByteArray());
            transaction.Sigs.Add(ByteString.CopyFrom(signature));
            
            await BroadcastTransactions(new List<Transaction> {transaction});
            await MinedOneBlock(chain);
            
            // Get abi
            chain = await _blockchainService.GetChainAsync();
            var newContractAddress = Address.BuildContractAddress(chain.Id, 3);
            var chainContext = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            var abi = await _smartContractExecutiveService.GetAbiAsync(chainContext, newContractAddress);

            // Get abi from rpc
            var response = await JsonCallAsJObject("/chain", "GetContractAbi",
                new {address = newContractAddress.GetFormatted()});
            var responseAddress = response["result"]["Address"].ToString();
            var responseAbi = response["result"]["Abi"].ToString();

            responseAddress.ShouldBe(newContractAddress.GetFormatted());
            responseAbi.ShouldBe(abi.ToByteArray().ToHex());
        }

        [Fact]
        public async Task Get_ContractAbi_ReturnInvalidAddress()
        {
            var invalidAddress = "InvalidAddress";
            var response = await JsonCallAsJObject("/chain", "GetContractAbi",
                new {address = invalidAddress});
            var responseCode = (long) response["error"]["code"];
            var responseMessage = response["error"]["message"].ToString();

            responseCode.ShouldBe(Error.InvalidAddress);
            responseMessage.ShouldBe(Error.Message[Error.InvalidAddress]);
        }

        [Fact]
        public async Task Get_ContractAbi_ReturnNotFound()
        {
            var notFoundAddress = Address.FromString("NotFound").GetFormatted();
            var response = await JsonCallAsJObject("/chain", "GetContractAbi",
                new {address = notFoundAddress});
            var responseCode = (long) response["error"]["code"];
            var responseMessage = response["error"]["message"].ToString();

            responseCode.ShouldBe(Error.NotFound);
            responseMessage.ShouldBe(Error.Message[Error.NotFound]);
        }

        [Fact]
        public async Task Broadcast_Transaction_Success()
        {
            // Generate a transaction
            var chain = await _blockchainService.GetChainAsync();
            var transaction = await GenerateTransferTransaction(chain);
            var transactionHash = transaction.GetHash();

            var response = await JsonCallAsJObject("/chain", "BroadcastTransaction",
                new {rawTransaction = transaction.ToByteArray().ToHex()});
            var responseTransactionId = response["result"]["TransactionId"].ToString();

            responseTransactionId.ShouldBe(transactionHash.ToHex());

            var existTransaction = await _txHub.GetExecutableTransactionSetAsync();
            existTransaction.Transactions[0].GetHash().ShouldBe(transactionHash);
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
            var chain = await _blockchainService.GetChainAsync();
            var transaction = await GenerateTransferTransaction(chain);
            transaction.Sigs.Clear();

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
            var chain = await _blockchainService.GetChainAsync();
            var transaction1 = await GenerateTransferTransaction(chain);
            var transaction2 = await GenerateTransferTransaction(chain);
            var transactions = new List<Transaction> {transaction1, transaction2};
            var rawTransactions = string.Join(',', transactions.Select(t => t.ToByteArray().ToHex()));

            var response = await JsonCallAsJObject("/chain", "BroadcastTransactions",
                new {rawTransactions = rawTransactions});
            var responseTransactionIds = response["result"].ToList();

            responseTransactionIds.Count.ShouldBe(2);

            var existTransaction = await _txHub.GetExecutableTransactionSetAsync();
            responseTransactionIds[0].ToString().ShouldBe(existTransaction.Transactions[0].GetHash().ToHex());
            responseTransactionIds[1].ToString().ShouldBe(existTransaction.Transactions[1].GetHash().ToHex());
        }

        [Fact]
        public async Task Get_TransactionResult_Success()
        {
            // Generate a transaction and broadcast
            var chain = await _blockchainService.GetChainAsync();
            var transaction = await GenerateTransferTransaction(chain);
            var transactionHex = transaction.GetHash().ToHex();
            await BroadcastTransactions(new List<Transaction> {transaction});

            // Before mined
            var response = await JsonCallAsJObject("/chain", "GetTransactionResult",
                new {transactionId = transactionHex});
            var responseTransactionId = response["result"]["TransactionId"].ToString();
            var responseStatus = response["result"]["Status"].ToString();

            responseTransactionId.ShouldBe(transactionHex);
            responseStatus.ShouldBe(TransactionResultStatus.Pending.ToString());

            await MinedOneBlock(chain);

            // After mined
            response = await JsonCallAsJObject("/chain", "GetTransactionResult",
                new {transactionId = transactionHex});
            responseTransactionId = response["result"]["TransactionId"].ToString();
            responseStatus = response["result"]["Status"].ToString();

            responseTransactionId.ShouldBe(transactionHex);
            responseStatus.ShouldBe(TransactionResultStatus.Mined.ToString());
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
            var chain = await _blockchainService.GetChainAsync();
            var transactions = new List<Transaction>();
            for (int i = 0; i < 20; i++)
            {
                transactions.Add(await GenerateTransferTransaction(chain));
            }

            await BroadcastTransactions(transactions);
            var block = await MinedOneBlock(chain);

            var response = await JsonCallAsJObject("/chain", "GetTransactionsResult",
                new {blockHash = block.GetHash().ToHex(), offset = 0, num = 15});

            var responseTransactionResults = response["result"].ToList();
            responseTransactionResults.Count.ShouldBe(15);

            response = await JsonCallAsJObject("/chain", "GetTransactionsResult",
                new {blockHash = block.GetHash().ToHex(), offset = 15, num = 15});

            responseTransactionResults = response["result"].ToList();
            responseTransactionResults.Count.ShouldBe(5);
        }

        [Fact]
        public async Task Get_BlockInfo_Success()
        {
            var chain = await _blockchainService.GetChainAsync();
            var transactions = new List<Transaction>();
            for (int i = 0; i < 3; i++)
            {
                transactions.Add(await GenerateTransferTransaction(chain));
            }

            await BroadcastTransactions(transactions);
            var block = await MinedOneBlock(chain);

            var response = await JsonCallAsJObject("/chain", "GetBlockInfo",
                new {blockHeight = 3, includeTransactions = true});
            var responseResult = response["result"];
            
            responseResult["BlockHash"].ToString().ShouldBe(block.GetHash().ToHex());
            responseResult["Header"]["PreviousBlockHash"].ToString()
                .ShouldBe(block.Header.PreviousBlockHash.ToHex());
            responseResult["Header"]["MerkleTreeRootOfTransactions"].ToString().ShouldBe(
                block.Header.MerkleTreeRootOfTransactions.ToHex
                    ());
            responseResult["Header"]["MerkleTreeRootOfWorldState"].ToString()
                .ShouldBe(block.Header.MerkleTreeRootOfWorldState.ToHex());
            responseResult["Header"]["SideChainTransactionsRoot"].ToString().ShouldBe(
                block.Header.BlockExtraData == null
                    ? string.Empty
                    : block.Header.BlockExtraData.SideChainTransactionsRoot.ToHex());
            ((ulong) responseResult["Header"]["Height"]).ShouldBe(block.Height);
            Convert.ToDateTime(responseResult["Header"]["Time"]).ShouldBe(block.Header.Time.ToDateTime());
            responseResult["Header"]["ChainId"].ToString().ShouldBe(ChainHelpers.ConvertChainIdToBase58(chain.Id));
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

        private async Task<Block> MinedOneBlock(Chain chain)
        {
            var block = await _minerService.MineAsync(chain.BestChainHash, chain.BestChainHeight,
                DateTime.UtcNow.AddMilliseconds(4000));

            return block;
        }

        private async Task<Transaction> GenerateTransferTransaction(Chain chain)
        {
            var newUserKeyPair = CryptoHelpers.GenerateKeyPair();

            var transaction = await GenerateTransaction(chain, Address.FromPublicKey(_userEcKeyPair.PublicKey),
                Address.BuildContractAddress(chain.Id, 2), nameof(TokenContract.Transfer),
                Address.FromPublicKey(newUserKeyPair.PublicKey), 10);

            var signature =
                CryptoHelpers.SignWithPrivateKey(_userEcKeyPair.PrivateKey, transaction.GetHash().DumpByteArray());
            transaction.Sigs.Add(ByteString.CopyFrom(signature));

            return transaction;
        }

        private async Task BroadcastTransactions(List<Transaction> transactions)
        {
            var rawTransactions = string.Join(',', transactions.Select(t => t.ToByteArray().ToHex()));
            await JsonCallAsJObject("/chain", "BroadcastTransactions",
                new {rawTransactions = rawTransactions});
        }

        private async Task InitAccountAmount()
        {
            var chain = await _blockchainService.GetChainAsync();
            var account = await _accountService.GetAccountAsync();

            var transaction = await GenerateTransaction(chain, account, Address.BuildContractAddress(chain.Id, 2),
                nameof(TokenContract.Transfer), Address.FromPublicKey(_userEcKeyPair.PublicKey), 10000);

            var signature = await _accountService.SignAsync(transaction.GetHash().DumpByteArray());
            transaction.Sigs.Add(ByteString.CopyFrom(signature));

            await BroadcastTransactions(new List<Transaction> {transaction});
            await MinedOneBlock(chain);
        }

        private async Task<Transaction> GenerateTransaction(Chain chain, Address from, Address to,
            string methodName, params object[] objects)
        {
            var transaction = new Transaction
            {
                From = from,
                To = to,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(objects)),
                RefBlockNumber = chain.BestChainHeight,
                RefBlockPrefix = ByteString.CopyFrom(chain.BestChainHash.DumpByteArray().Take(4).ToArray())
            };

            return transaction;
        }
    }
}