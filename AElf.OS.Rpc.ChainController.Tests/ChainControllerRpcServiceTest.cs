﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Common.Application;
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
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.Token;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types.CSharp;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Shouldly;
using Volo.Abp.Threading;
using Volo.Abp.Validation;
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
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ChainControllerRpcServiceServerTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            Logger = GetService<ILogger<ChainControllerRpcServiceServerTest>>() ??
                     NullLogger<ChainControllerRpcServiceServerTest>.Instance;

            _blockchainService = GetRequiredService<IBlockchainService>();
            _txHub = GetRequiredService<ITxHub>();
            _minerService = GetRequiredService<IMinerService>();
            _smartContractExecutiveService = GetRequiredService<ISmartContractExecutiveService>();
            _accountService = GetRequiredService<IAccountService>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();

            _userEcKeyPair = CryptoHelpers.GenerateKeyPair();

            AsyncHelper.RunSync(async () => await InitAccountAmount());
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
            var basicContractZero = _smartContractAddressService.GetZeroSmartContractAddress();

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
            var transaction = GenerateTransaction(chain, Address.FromPublicKey(_userEcKeyPair.PublicKey),
                _smartContractAddressService.GetZeroSmartContractAddress(),
                nameof(ISmartContractZero.DeploySmartContract), 2,
                File.ReadAllBytes(typeof(BasicContractZero).Assembly.Location));
            var signature =
                CryptoHelpers.SignWithPrivateKey(_userEcKeyPair.PrivateKey, transaction.GetHash().DumpByteArray());
            transaction.Sigs.Add(ByteString.CopyFrom(signature));

            await BroadcastTransactions(new List<Transaction> {transaction});
            await MinedOneBlock(chain);

            // Get abi
            chain = await _blockchainService.GetChainAsync();
            var newContractAddress = _smartContractAddressService.GetZeroSmartContractAddress();
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

        [Fact(Skip = "https://github.com/AElfProject/AElf/issues/1105")]
        public async Task Call_ReadOnly_Success()
        {
            // Generate a transaction
            var chain = await _blockchainService.GetChainAsync();
            var transaction = await GenerateViewTransaction(chain, "Symbol");
            var transactionHash = transaction.GetHash();

            var response = await JsonCallAsJObject("/chain", "Call",
                new {rawTransaction = transaction.ToByteArray().ToHex()});
            response["result"].ToString().ShouldBe("");
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

            response = await JsonCallAsJObject("/chain", "GetTransactionPoolStatus");
            response["result"]["Queued"].ShouldBe(2);
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
        public async Task Get_Failed_TransactionResult_Success()
        {
            // Generate a transaction and broadcast
            var chain = await _blockchainService.GetChainAsync();
            var transactionList = await GenerateTwoInitializeTransaction(chain);
            await BroadcastTransactions(transactionList);

            var block = await MinedOneBlock(chain);

            // After executed
            var transactionHex = transactionList[1].GetHash().ToHex();
            var response = await JsonCallAsJObject("/chain", "GetTransactionResult",
                new {transactionId = transactionHex});
            var responseTransactionId = response["result"]["TransactionId"].ToString();
            var responseStatus = response["result"]["Status"].ToString();
            var responseErrorMessage = response["result"]["Error"].ToString();

            responseTransactionId.ShouldBe(transactionHex);
            responseStatus.ShouldBe(TransactionResultStatus.Failed.ToString());
            responseErrorMessage.Contains("Already initialized.").ShouldBeTrue();
        }
        
        [Fact(Skip = "https://github.com/AElfProject/AElf/issues/1083")]
        public async Task Get_NotExisted_TransactionResult()
        {
            // Generate a transaction
            var chain = await _blockchainService.GetChainAsync();
            var transaction = await GenerateTransferTransaction(chain);
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
            var chain = await _blockchainService.GetChainAsync();
            var transactions = new List<Transaction>();
            for (int i = 0; i < 20; i++)
            {
                transactions.Add(await GenerateTransferTransaction(chain));
            }

            await BroadcastTransactions(transactions);
            var block = await MinedOneBlock(chain);

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
            var block = new Block
            {
                Header = new BlockHeader(),
                BlockHashToHex = "Test"
            };
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
            var block = new Block
            {
                Header = new BlockHeader(),
                BlockHashToHex = "Test"
            };
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
            ((long) responseResult["Header"]["Height"]).ShouldBe(block.Height);
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

        [Fact]
        public async Task Get_Chain_Status_Success()
        {
            var response = await JsonCallAsJObject("/chain", "GetChainStatus");
            response["result"]["Branches"].ShouldNotBeNull();
            Convert.ToInt32(response["result"]["BestChainHeight"]).ShouldBe(2);
        }

        [Fact]
        public async Task Get_Block_State_Success()
        {
            var chain = await _blockchainService.GetChainAsync();
            var transactions = new List<Transaction>();
            for (int i = 0; i < 3; i++)
            {
                transactions.Add(await GenerateTransferTransaction(chain));
            }

            await BroadcastTransactions(transactions);
            await MinedOneBlock(chain);

            var response = await JsonCallAsJObject("/chain", "GetBlockInfo",
                new {blockHeight = 3, includeTransactions = true});
            var responseResult = response["result"];
            var blockHash = responseResult["BlockHash"].ToString();

            var response1 = await JsonCallAsJObject("/chain", "GetBlockState",
                new {blockHash = blockHash});
            response1["result"].ShouldNotBeNull();
            response1["result"]["BlockHash"].ToString().ShouldBe(blockHash);
            response1["result"]["BlockHeight"].To<int>().ShouldBe(3);
            response1["result"]["Changes"].ShouldNotBeNull();
        }

        [Fact]
        public async Task Get_Block_State_FewBlocks_Later()
        {
            var chain = await _blockchainService.GetChainAsync();
            var transactions = new List<Transaction>();
            for (int i = 0; i < 3; i++)
            {
                transactions.Add(await GenerateTransferTransaction(chain));
            }

            await BroadcastTransactions(transactions);
            await MinedOneBlock(chain);

            var response = await JsonCallAsJObject("/chain", "GetBlockInfo",
                new {blockHeight = 3, includeTransactions = true});
            var responseResult = response["result"];
            var blockHash = responseResult["BlockHash"].ToString();

            //Continue generate block 
            for (int i = 0; i < 10; i++)
            {
                await MinedOneBlock(chain);
            }

            //Check Block State
            var response1 = await JsonCallAsJObject("/chain", "GetBlockState",
                new {blockHash = blockHash});
            response1["result"].ShouldNotBeNull();
            response1["result"]["BlockHash"].ToString().ShouldBe(blockHash);
            response1["result"]["BlockHeight"].To<int>().ShouldBe(3);
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
        public async Task Transaction_To_JObject()
        {
            var chain = await _blockchainService.GetChainAsync();
            var transaction = GenerateTransaction(chain, Address.Generate(), Address.Generate(),
                nameof(TokenContract.Transfer),
                Address.Generate(), 1000UL);
            var transactionObj = transaction.GetTransactionInfo();
            transactionObj.ShouldNotBeNull();
            transactionObj["Transaction"]["Method"].ToString().ShouldBe(nameof(TokenContract.Transfer));
        }

        [Fact]
        public async Task Get_FileDescriptorSet_Success()
        {
            // Generate a transaction and broadcast
            var chain = await _blockchainService.GetChainAsync();
            var transaction = await GenerateTransferTransaction(chain);
            await BroadcastTransactions(new List<Transaction> {transaction});
            
            await MinedOneBlock(chain);
            
            //Result empty
            var response = await JsonCallAsJObject("/chain", "GetFileDescriptorSet",
                new {address = transaction.To.GetFormatted()});
            response["result"].ToString().ShouldBeEmpty();
            
            //Result not empty
            //TODO: Add logic to cover query with data case [Case] 
            //BODY: Will complete this logic after contract changed to grpc style. 
        }

        [Fact]
        public async Task Get_FileDescriptorSet_Failed()
        {
            var addressInfo = Address.Generate().GetFormatted();
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

        #region Wallet cases

        [Fact]
        public async Task Wallet_ListAccounts()
        {
            var chain = await _blockchainService.GetChainAsync();
            var walletStorePath = Path.Combine(ApplicationHelper.AppDataPath, "rpc-managed-wallet");
            var store = new AElfKeyStore(walletStorePath);
            var keyPair = await store.CreateAsync("123", chain.Id.ToString());
            var addressString = Address.FromPublicKey(keyPair.PublicKey).GetFormatted();

            var response = await JsonCallAsJObject("/wallet", "ListAccounts");
            response.ShouldNotBeNull();
            response["result"].ToList().Count.ShouldBeGreaterThanOrEqualTo(1);
            response["result"].ToList().Contains(addressString).ShouldBeTrue();

            Directory.Delete(walletStorePath, true);
        }

        [Fact]
        public async Task Wallet_SignHash_Success()
        {
            var chain = await _blockchainService.GetChainAsync();
            var walletStorePath = Path.Combine(ApplicationHelper.AppDataPath, "rpc-managed-wallet");
            var store = new AElfKeyStore(walletStorePath);
            var keyPair = await store.CreateAsync("123", chain.Id.ToString());
            var addressString = Address.FromPublicKey(keyPair.PublicKey).GetFormatted();

            var response = await JsonCallAsJObject("/wallet", "SignHash",
                new {address = addressString, password = "123", hash = Hash.Generate().ToHex()});
            response.ShouldNotBeNull();
            response["result"].ToString().ShouldNotBeEmpty();

            Directory.Delete(walletStorePath, true);
        }

        [Fact]
        public async Task Wallet_SignHash_Failed()
        {
            var chain = await _blockchainService.GetChainAsync();
            var walletStorePath = Path.Combine(ApplicationHelper.AppDataPath, "rpc-managed-wallet");
            var store = new AElfKeyStore(walletStorePath);
            var keyPair = await store.CreateAsync("123", chain.Id.ToString());
            var addressString = Address.FromPublicKey(keyPair.PublicKey).GetFormatted();

            var response = await JsonCallAsJObject("/wallet", "SignHash",
                new {address = addressString, password = "wrong_password", hash = Hash.Generate().ToHex()});
            response.ShouldNotBeNull();
            response["error"]["code"].To<long>().ShouldBe(Wallet.Error.WrongPassword);
            response["error"]["message"].ToString().ShouldBe(Wallet.Error.Message[Wallet.Error.WrongPassword]);

            response = await JsonCallAsJObject("/wallet", "SignHash",
                new {address = addressString + "test", password = "123", hash = Hash.Generate().ToHex()});
            response.ShouldNotBeNull();
            response["error"]["code"].To<long>().ShouldBe(Wallet.Error.AccountNotExist);
            response["error"]["message"].ToString().ShouldBe(Wallet.Error.Message[Wallet.Error.AccountNotExist]);

            Directory.Delete(walletStorePath, true);
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

        private async Task<Block> MinedOneBlock(Chain chain)
        {
            var block = await _minerService.MineAsync(chain.BestChainHash, chain.BestChainHeight,
                DateTime.UtcNow.AddMilliseconds(4000));

            return block;
        }

        private async Task<Transaction> GenerateTransferTransaction(Chain chain)
        {
            var newUserKeyPair = CryptoHelpers.GenerateKeyPair();

            var transaction = GenerateTransaction(chain, Address.FromPublicKey(_userEcKeyPair.PublicKey),
                _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name),
                nameof(TokenContract.Transfer),
                Address.FromPublicKey(newUserKeyPair.PublicKey), 10);

            var signature =
                CryptoHelpers.SignWithPrivateKey(_userEcKeyPair.PrivateKey, transaction.GetHash().DumpByteArray());
            transaction.Sigs.Add(ByteString.CopyFrom(signature));

            return transaction;
        }

        private async Task<List<Transaction>> GenerateTwoInitializeTransaction(Chain chain)
        {
            var transactionList = new List<Transaction>();
            var newUserKeyPair = CryptoHelpers.GenerateKeyPair();

            for (int i = 0; i < 2; i++)
            {
                var transaction = GenerateTransaction(chain, Address.FromPublicKey(_userEcKeyPair.PublicKey),
                    _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name),
                    nameof(TokenContract.Initialize),
                    "AELF", "elf token", 1000_000, 2);

                var signature =
                    CryptoHelpers.SignWithPrivateKey(_userEcKeyPair.PrivateKey, transaction.GetHash().DumpByteArray());
                transaction.Sigs.Add(ByteString.CopyFrom(signature));

                transactionList.Add(transaction); 
            }
            
            return transactionList;
        }

        private async Task<Transaction> GenerateViewTransaction(Chain chain, string method, params object[] paramArray)
        {
            var newUserKeyPair = CryptoHelpers.GenerateKeyPair();

            var transaction = GenerateTransaction(chain, Address.FromPublicKey(_userEcKeyPair.PublicKey),
                _smartContractAddressService.GetAddressByContractName(Hash.FromString(typeof(TokenContract).FullName)),
                method,
                paramArray);

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

            var transaction = GenerateTransaction(chain, account,
                _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name),
                nameof(TokenContract.Transfer), Address.FromPublicKey(_userEcKeyPair.PublicKey), 10000);

            var signature = await _accountService.SignAsync(transaction.GetHash().DumpByteArray());
            transaction.Sigs.Add(ByteString.CopyFrom(signature));

            await BroadcastTransactions(new List<Transaction> {transaction});
            await MinedOneBlock(chain);
        }

        private Transaction GenerateTransaction(Chain chain, Address from, Address to,
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