using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.CrossChain;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Types.CSharp;
using Google.Protobuf;
using Xunit;

namespace AElf.Contract.CrossChain.Tests
{
    /*
     * Todo:
     * Side chain creation proposal is disable.
     * Lock resource is disable.
     * Token amount to check.
    */
    public class SideChainLifeTimeManagementTest : CrossChainContractTestBase
    {
        [Fact]
        public async Task Request_SideChain_Creation()
        {
            await InitializeCrossChainContract();
            long lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            var balanceResult = await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.GetBalance),
                new GetBalanceInput
                {
                    Owner = Address.FromPublicKey(Tester.KeyPair.PublicKey),
                    Symbol = "ELF"
                });
            Assert.Equal(800_000L, balanceResult.DeserializeToPbMessage<GetBalanceOutput>().Balance);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            var txResult = await ExecuteContractWithMiningAsync(CrossChainContractAddress, nameof(CrossChainContract.RequestChainCreation),
                sideChainInfo);
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Mined);
            var expectedChainId = ChainHelpers.GetChainId(1);
            Assert.Equal(expectedChainId, txResult.ReturnValue.DeserializeToInt32());
        }
        
        [Fact]
        public async Task Request_SideChain_Creation_WithoutApprove()
        {
            await InitializeCrossChainContract();
            long lockedTokenAmount = 10;
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            var txResult = await ExecuteContractWithMiningAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName,
                sideChainInfo);
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }
        
        [Fact]
        public async Task Request_SideChain_Creation_WithoutEnoughToken()
        {
            await InitializeCrossChainContract();
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = 1000_000L
            };
            var txResult = await ExecuteContractWithMiningAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName,
                sideChainInfo);
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task Request_SideChain_Creation_WrongProposer()
        {
            await InitializeCrossChainContract();
            long lockedTokenAmount = 10;
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                LockedTokenAmount = lockedTokenAmount,
                Proposer = Address.Generate()
            };
            var txResult = await ExecuteContractWithMiningAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName,
                sideChainInfo);
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }
        
        [Fact]
        public async Task Request_SideChain_Creation_WrongStatus()
        {
            await InitializeCrossChainContract();
            long lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Review,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress()
            };
            var txResult = await ExecuteContractWithMiningAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName,
                sideChainInfo);
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }
        
        [Fact]
        public async Task Request_SideChain_Creation_Twice()
        {
            await InitializeCrossChainContract();
            long lockedTokenAmount = 20;
            await ApproveBalance(lockedTokenAmount * 2);

            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            var tx = await GenerateTransactionAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName, null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx});
            
            var txResult = await ExecuteContractWithMiningAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName,
                sideChainInfo);
            var expectedChainId = ChainHelpers.GetChainId(2);
            Assert.Equal(expectedChainId, txResult.ReturnValue.DeserializeToInt32());
        }

        [Fact]
        public async Task Withdraw_ChainCreation()
        {
            await InitializeCrossChainContract();
            long lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx = await GenerateTransactionAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName, null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx});
            var expectedChainId = ChainHelpers.GetChainId(1);
            var txResult =
                await ExecuteContractWithMiningAsync(CrossChainContractAddress, "WithdrawRequest", expectedChainId);
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Mined);
        }
        
        [Fact]
        public async Task Withdraw_ChainCreation_WithWrongSender()
        {
            await InitializeCrossChainContract();
            long lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            
            var ecKeyPair = CryptoHelpers.GenerateKeyPair();
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = Address.FromPublicKey(ecKeyPair.PublicKey),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx = await GenerateTransactionAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName, ecKeyPair,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx});
            var expectedChainId = ChainHelpers.GetChainId(1);
            var txResult =
                await ExecuteContractWithMiningAsync(CrossChainContractAddress, "WithdrawRequest", expectedChainId);
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }
        
        [Fact]
        public async Task Withdraw_ChainCreation_ChainNotExist()
        {
            await InitializeCrossChainContract();
            long lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx = await GenerateTransactionAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName, null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx});
            var expectedChainId = ChainHelpers.GetChainId(2);
            var txResult =
                await ExecuteContractWithMiningAsync(CrossChainContractAddress, "WithdrawRequest", expectedChainId);
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }
        
        [Fact]
        public async Task Withdraw_ChainCreation_WrongStatus()
        {
            await InitializeCrossChainContract();
            long lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx = await GenerateTransactionAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName, null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx});
            var chainId = ChainHelpers.GetChainId(1);
            await ExecuteContractWithMiningAsync(CrossChainContractAddress, "CreateSideChain", chainId);
            var txResult =
                await ExecuteContractWithMiningAsync(CrossChainContractAddress, "WithdrawRequest", chainId);
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task Create_SideChain()
        {
            await InitializeCrossChainContract();
            long lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx = await GenerateTransactionAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName,null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx});
            var chainId = ChainHelpers.GetChainId(1);
            var txResult =
                await ExecuteContractWithMiningAsync(CrossChainContractAddress, "CreateSideChain", chainId);
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Mined);
            var bytes = await CallContractMethodAsync(CrossChainContractAddress, CrossChainConsts.GetAllChainsIdAndHeightMethodName);
            var dict = new Dictionary<int, long>(bytes.DeserializeToPbMessage<SideChainIdAndHeightDict>().IdHeightDict);
            Assert.True(dict.ContainsKey(chainId));
        }
        
        [Fact]
        public async Task CheckLockedBalance()
        {
            await InitializeCrossChainContract();
            long lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var requestTransaction = await GenerateTransactionAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName,null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {requestTransaction});
            var chainId = ChainHelpers.GetChainId(1);
            var creationTransaction = await GenerateTransactionAsync(CrossChainContractAddress, "CreateSideChain", null, chainId);
            await MineAsync(new List<Transaction> {creationTransaction});
            var balance = await CallContractMethodAsync(CrossChainContractAddress, CrossChainConsts.GetLockedBalanceMethodName, chainId);
            Assert.Equal(10, balance.DeserializeToInt64());
            
        }
        
        [Fact(Skip = "Event is not used.")]
        public async Task Create_SideChain_FireEvent()
        {
            long lockedTokenAmount = 10;           
            await InitializeCrossChainContract();
            await ApproveBalance(lockedTokenAmount);
            
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx = await GenerateTransactionAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName, null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx});
            var txRes = await GetTransactionResult(tx.GetHash());
            var chainId = ChainHelpers.GetChainId(1);
            var txResult =
                await ExecuteContractWithMiningAsync(CrossChainContractAddress, "CreateSideChain", chainId);
            Assert.True(txResult.Status == TransactionResultStatus.Mined);
            object[] data = ParamsPacker.Unpack(txResult.Logs.First().Data.ToByteArray(),
                new[] {typeof(Address), typeof(int)});
            var actualChainId = (int) data[1];
            var actualSender = (Address) data[0];
            Assert.True(chainId == actualChainId);
            Assert.Equal(CrossChainContractTestHelper.GetAddress(), actualSender);    
        }
        
        [Fact]
        public async Task Create_SideChain_ChainNotExist()
        {
            long lockedTokenAmount = 10;           
            await InitializeCrossChainContract();
            await ApproveBalance(lockedTokenAmount);
            
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress()
            };
            
            var tx = await GenerateTransactionAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName, null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx});
            var chainId = ChainHelpers.GetChainId(2);
            var txResult =
                await ExecuteContractWithMiningAsync(CrossChainContractAddress, "CreateSideChain", chainId);
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task Request_SideChain_Disposal()
        {
            long lockedTokenAmount = 10;           
            await InitializeCrossChainContract();
            await ApproveBalance(lockedTokenAmount);
            
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx1 = await GenerateTransactionAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName, null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx1});
            var chainId = ChainHelpers.GetChainId(1);
            var tx2 = await GenerateTransactionAsync(CrossChainContractAddress, "CreateSideChain", null, chainId);
            await MineAsync(new List<Transaction> {tx2});
            var txResult =
                await ExecuteContractWithMiningAsync(CrossChainContractAddress, "RequestChainDisposal", chainId);
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Mined);
        }
        
        [Fact]
        public async Task Dispose_SideChain()
        {
            long lockedTokenAmount = 10;           
            await InitializeCrossChainContract();
            await ApproveBalance(lockedTokenAmount);
            
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx1 = await GenerateTransactionAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName,null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx1});
            var chainId = ChainHelpers.GetChainId(1);
            var tx2 = await GenerateTransactionAsync(CrossChainContractAddress, "CreateSideChain", null, chainId);
            await MineAsync(new List<Transaction> {tx2});
            await ExecuteContractWithMiningAsync(CrossChainContractAddress, "RequestChainDisposal", chainId);
            var txResult2 =
                await ExecuteContractWithMiningAsync(CrossChainContractAddress, "DisposeSideChain", chainId);
            object[] data = ParamsPacker.Unpack(txResult2.Logs.First().Data.ToByteArray(),
                new[] {typeof(int)});
            var disposedChainId = (int) data[0];
            Assert.True(chainId == disposedChainId);
        }

        [Fact]
        public async Task GetChainStatus_Review()
        {
            long lockedTokenAmount = 10;           
            await InitializeCrossChainContract();
            await ApproveBalance(lockedTokenAmount);
            
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx1 = await GenerateTransactionAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName, null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx1});
            var chainId = ChainHelpers.GetChainId(1);
            
            var status = await CallContractMethodAsync(CrossChainContractAddress, "GetChainStatus", chainId);
            Assert.Equal(1, status.DeserializeToInt32());
        }
        
        [Fact]
        public async Task GetChainStatus_Active()
        {
            long lockedTokenAmount = 10;           
            await InitializeCrossChainContract();
            await ApproveBalance(lockedTokenAmount);
            
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx1 = await GenerateTransactionAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName, null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx1});
            var chainId = ChainHelpers.GetChainId(1);
            await ExecuteContractWithMiningAsync(CrossChainContractAddress, "CreateSideChain", chainId);
            var status = await CallContractMethodAsync(CrossChainContractAddress, "GetChainStatus", chainId);
            Assert.True(status.DeserializeToInt32() == 2);
        }
        
        [Fact]
        public async Task GetChainStatus_ChainNotExist()
        {
            long lockedTokenAmount = 10;           
            await InitializeCrossChainContract();
            await ApproveBalance(lockedTokenAmount);
            
            var chainId = ChainHelpers.GetChainId(1);
            var status = await CallContractMethodAsync(CrossChainContractAddress, "GetChainStatus", chainId);
            Assert.Equal(ByteString.Empty, status);
        }

        [Fact]
        public async Task Get_SideChain_Height()
        {
            long lockedTokenAmount = 10;           
            await InitializeCrossChainContract();
            await ApproveBalance(lockedTokenAmount);
            
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx1 = await GenerateTransactionAsync(CrossChainContractAddress, CrossChainConsts.RequestChainCreationMethodName, null,
                sideChainInfo);
            await MineAsync(new List<Transaction> {tx1});
            var chainId = ChainHelpers.GetChainId(1);
            await ExecuteContractWithMiningAsync(CrossChainContractAddress, "CreateSideChain", chainId);
            var height = await CallContractMethodAsync(CrossChainContractAddress, "GetSideChainHeight", chainId);
            Assert.True(height.DeserializeToInt32() == 0);
        }
        
        [Fact]
        public async Task Get_SideChain_Height_ChainNotExist()
        {
            long lockedTokenAmount = 10;           
            await InitializeCrossChainContract();
            await ApproveBalance(lockedTokenAmount);
            var chainId = ChainHelpers.GetChainId(1);
            var height = await CallContractMethodAsync(CrossChainContractAddress, "GetSideChainHeight", chainId);
            Assert.Equal(ByteString.Empty, height);
        }
    }
}