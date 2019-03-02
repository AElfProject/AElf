using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Genesis;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types.CSharp;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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

        private readonly int _chainId;
        private ECKeyPair _keyPair;

        public ChainControllerRpcServiceServerTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            Logger = GetService<ILogger<ChainControllerRpcServiceServerTest>>() ??
                     NullLogger<ChainControllerRpcServiceServerTest>.Instance;

            _blockchainService = GetRequiredService<IBlockchainService>();
            _txHub = GetRequiredService<ITxHub>();
            _minerService = GetRequiredService<IMinerService>();
            _smartContractExecutiveService = GetRequiredService<ISmartContractExecutiveService>();
            _accountService = GetRequiredService<IAccountService>();

            _chainId = GetRequiredService<IOptionsSnapshot<ChainOptions>>().Value.ChainId;

            _keyPair = CryptoHelpers.GenerateKeyPair();
            
            AsyncHelper.RunSync( async () => { InitAccountAmount(); });
        }

        [Fact]
        public async Task Get_BlockHeight_Success()
        {
            // Get current height
            var response = await JsonCallAsJObject("/chain", "GetBlockHeight");
            var height = (int) response["result"];
            height.ShouldBe(2);

            // Mined one block
            var chain = await _blockchainService.GetChainAsync(_chainId);
            var tx = await GenerateTransferTransaction(chain);
            await MinedOneBlock(chain, new List<Transaction> {tx});

            // Get latest heigh
            response = await JsonCallAsJObject("/chain", "GetBlockHeight");
            height = (int) response["result"];
            height.ShouldBe(3);
        }

        [Fact]
        public async Task Connect_Chain_Success()
        {
            var basicContractZero = Address.BuildContractAddress(_chainId, 0);

            var response = await JsonCallAsJObject("/chain", "ConnectChain");

            var zeroContractAddress = response["result"][SmartContract.GenesisSmartContractZeroAssemblyName].ToString();
            var chainId = ChainHelpers.ConvertBase58ToChainId(response["result"]["ChainId"].ToString());

            zeroContractAddress.ShouldBe(basicContractZero.GetFormatted());
            chainId.ShouldBe(_chainId);
        }

        [Fact]
        public async Task Get_ContractAbi_Success()
        {
            // Deploy a new contact and mined
            var chain = await _blockchainService.GetChainAsync(_chainId);
            var tx = await GenerateTransaction(chain.BestChainHash, Address.FromPublicKey(_keyPair.PublicKey),
                Address.BuildContractAddress(_chainId, 0), "DeploySmartContract", 2,
                File.ReadAllBytes(typeof(BasicContractZero).Assembly.Location));
            await MinedOneBlock(chain, new List<Transaction> {tx});

            // Get abi
            var newContractAddress = Address.BuildContractAddress(_chainId, 1);
            var chainContext = new ChainContext
            {
                ChainId = _chainId,
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            var abi = await _smartContractExecutiveService.GetAbiAsync(_chainId, chainContext, newContractAddress);

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

        [Fact(Skip = "Should handle the situation contract doesn't exist")]
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
            var chain = await _blockchainService.GetChainAsync(_chainId);
            var tx = await GenerateTransferTransaction(chain);
            var txHash = tx.GetHash();

            var response = await JsonCallAsJObject("/chain", "BroadcastTransaction",
                new {rawTransaction = tx.ToByteArray().ToHex()});
            var responseTxId = response["result"]["TransactionId"].ToString();

            responseTxId.ShouldBe(txHash.ToHex());

            var existTx = await _txHub.GetExecutableTransactionSetAsync();
            existTx.Transactions[0].GetHash().ShouldBe(txHash);
        }

        [Fact]
        public async Task Broadcast_Transaction_ReturnInvalidTransaction()
        {
            var fakeTx = "FakeTx";
            var response = await JsonCallAsJObject("/chain", "BroadcastTransaction",
                new {rawTransaction = fakeTx});
            var responseCode = (long) response["error"]["code"];
            var responseMessage = response["error"]["message"].ToString();

            responseCode.ShouldBe(Error.InvalidTransaction);
            responseMessage.ShouldBe(Error.Message[Error.InvalidTransaction]);
        }

        [Fact]
        public async Task Broadcast_UnableVerify_Transaction_ReturnInvalidTransaction()
        {
            var chain = await _blockchainService.GetChainAsync(_chainId);
            var tx = await GenerateTransferTransaction(chain);
            tx.Sigs.Clear();

            var response = await JsonCallAsJObject("/chain", "BroadcastTransaction",
                new {rawTransaction = tx.ToByteArray().ToHex()});
            var responseCode = (long) response["error"]["code"];
            var responseMessage = response["error"]["message"].ToString();

            responseCode.ShouldBe(Error.InvalidTransaction);
            responseMessage.ShouldBe(Error.Message[Error.InvalidTransaction]);

            var existTx = await _txHub.GetTransactionReceiptAsync(tx.GetHash());
            existTx.ShouldBeNull();
        }

        [Fact]
        public async Task Broadcast_Transactions_Success()
        {
            var chain = await _blockchainService.GetChainAsync(_chainId);
            var tx1 = await GenerateTransferTransaction(chain);
            var tx2 = await GenerateTransferTransaction(chain);
            var txs = new List<Transaction> {tx1, tx2};
            var rawTxs = string.Join(',', txs.Select(t => t.ToByteArray().ToHex()));

            var response = await JsonCallAsJObject("/chain", "BroadcastTransactions",
                new {rawTransactions = rawTxs});
            var responseTxIds = response["result"].ToList();

            responseTxIds.Count.ShouldBe(2);

            var existTx = await _txHub.GetExecutableTransactionSetAsync();
            responseTxIds[0].ToString().ShouldBe(existTx.Transactions[0].GetHash().ToHex());
            responseTxIds[1].ToString().ShouldBe(existTx.Transactions[1].GetHash().ToHex());
        }

        [Fact]
        public async Task Get_TransactionResult_Success()
        {
            var chain = await _blockchainService.GetChainAsync(_chainId);
            var tx = await GenerateTransferTransaction(chain);
            var txHex = tx.GetHash().ToHex();
            
            await JsonCallAsJObject("/chain", "BroadcastTransaction",
                new {rawTransaction = tx.ToByteArray().ToHex()});
            
            var response = await JsonCallAsJObject("/chain", "GetTransactionResult",
                new {transactionId = txHex});
            var responseTxId = response["result"]["TransactionId"].ToString();
            var responseStatus = response["result"]["Status"].ToString();
            
            responseTxId.ShouldBe(txHex);
            responseStatus.ShouldBe(TransactionResultStatus.Pending.ToString());
            
            await MinedOneBlock(chain, new List<Transaction> {tx});
            
            response = await JsonCallAsJObject("/chain", "GetTransactionResult",
                new {transactionId = txHex});
            responseTxId = response["result"]["TransactionId"].ToString();
            responseStatus = response["result"]["Status"].ToString();
            
            responseTxId.ShouldBe(txHex);
            responseStatus.ShouldBe(TransactionResultStatus.Mined.ToString());
        }
        
        [Fact]
        public async Task Get_TransactionResult_ReturnInvalidTransactionId()
        {
            var fakeTxId = "FakeTxId";
            var response = await JsonCallAsJObject("/chain", "GetTransactionResult",
                new {transactionId = fakeTxId});
            var responseCode = (long) response["error"]["code"];
            var responseMessage = response["error"]["message"].ToString();

            responseCode.ShouldBe(Error.InvalidTransactionId);
            responseMessage.ShouldBe(Error.Message[Error.InvalidTransactionId]);
        }
        
        

        private async Task<Block> MinedOneBlock(Chain chain, List<Transaction> txs)
        {
            foreach (var tx in txs)
            {
                await _txHub.AddTransactionAsync(_chainId, tx);
            }

            var block = await _minerService.MineAsync(_chainId, chain.BestChainHash, chain.BestChainHeight,
                DateTime.UtcNow.AddMilliseconds(4000));

            return block;
        }

        private async Task<Transaction> GenerateTransferTransaction(Chain chain)
        {
            var newUserKeyPair = CryptoHelpers.GenerateKeyPair();

            var tx = await GenerateTransaction(chain.BestChainHash, Address.FromPublicKey(_keyPair.PublicKey),Address.BuildContractAddress(_chainId, 2),
                "Transfer", Address.FromPublicKey(newUserKeyPair.PublicKey), 100);

            var signature = CryptoHelpers.SignWithPrivateKey(_keyPair.PrivateKey, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature));

            return tx;
        }

        private async Task InitAccountAmount()
        {
            var chain = await _blockchainService.GetChainAsync(_chainId);
            var account = await _accountService.GetAccountAsync();
            var publicKey = await _accountService.GetPublicKeyAsync();

            var tx = await GenerateTransaction(chain.BestChainHash, account,Address.BuildContractAddress(_chainId, 2),
                "Transfer", Address.FromPublicKey(_keyPair.PublicKey), 100);

            var signature = CryptoHelpers.SignWithPrivateKey(publicKey, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature));

            await MinedOneBlock(chain, new List<Transaction> {tx});
        }

        private async Task<Transaction> GenerateTransaction(Hash bestChainHash, Address from, Address to,
            string methodName, params object[] objects)
        {
            var tx = new Transaction
            {
                From = from,
                To = to,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(objects)),
                RefBlockNumber = _blockchainService.GetBestChainLastBlock(_chainId).Result.Height,
                RefBlockPrefix = ByteString.CopyFrom(bestChainHash.DumpByteArray().Take(4).ToArray())
            };

            return tx;
        }
    }
}