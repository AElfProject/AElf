using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf.Common;
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
            await Initialize(1000_000UL);
            ulong lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            var txResult = await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "RequestChainCreation",
                sideChainInfo);
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Mined);
            var expectedChainId = ChainHelpers.ConvertChainIdToBase58(ChainHelpers.GetChainId(1));
            var actualChainId = txResult.RetVal.ToStringUtf8();
            Assert.Equal(expectedChainId, actualChainId);
        }
        
        [Fact]
        public async Task Request_SideChain_Creation_WithoutApprove()
        {
            await Initialize(1000_000UL);
            ulong lockedTokenAmount = 10;
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            var txResult = await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "RequestChainCreation",
                sideChainInfo);
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }
        
        [Fact]
        public async Task Request_SideChain_Creation_WithoutEnoughToken()
        {
            await Initialize(1000);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = 1000_000UL
            };
            var txResult = await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "RequestChainCreation",
                sideChainInfo);
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task Request_SideChain_Creation_WrongProposer()
        {
            await Initialize(1000_000UL);
            ulong lockedTokenAmount = 10;
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                LockedTokenAmount = lockedTokenAmount,
                Proposer = Address.Generate()
            };
            var txResult = await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "RequestChainCreation",
                sideChainInfo);
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }
        
        [Fact]
        public async Task Request_SideChain_Creation_WrongStatus()
        {
            await Initialize(1000);
            ulong lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Review,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress()
            };
            var txResult = await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "RequestChainCreation",
                sideChainInfo);
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }
        
        [Fact]
        public async Task Request_SideChain_Creation_Twice()
        {
            await Initialize(1000);
            ulong lockedTokenAmount = 20;
            await ApproveBalance(lockedTokenAmount * 2);

            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = lockedTokenAmount,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            var tx = ContractTester.GenerateTransaction(CrossChainContractAddress, "RequestChainCreation",
                sideChainInfo);
            await ContractTester.MineABlockAsync(new List<Transaction> {tx});
            
            var txResult = await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "RequestChainCreation",
                sideChainInfo);
            var expectedChainId = ChainHelpers.ConvertChainIdToBase58(ChainHelpers.GetChainId(2));
            var actualChainId = txResult.RetVal.ToStringUtf8();
            Assert.Equal(expectedChainId, actualChainId);
        }

        [Fact]
        public async Task Withdraw_ChainCreation()
        {
            await Initialize(1000);
            ulong lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx = ContractTester.GenerateTransaction(CrossChainContractAddress, "RequestChainCreation",
                sideChainInfo);
            await ContractTester.MineABlockAsync(new List<Transaction> {tx});
            var expectedChainId = ChainHelpers.GetChainId(1);
            var txResult =
                await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "WithdrawRequest",
                    ChainHelpers.ConvertChainIdToBase58(expectedChainId));
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Mined);
        }
        
        [Fact]
        public async Task Withdraw_ChainCreation_WithWrongSender()
        {
            await Initialize(1000);
            ulong lockedTokenAmount = 10;
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
            
            var tx = ContractTester.GenerateTransaction(CrossChainContractAddress, "RequestChainCreation", ecKeyPair,
                sideChainInfo);
            await ContractTester.MineABlockAsync(new List<Transaction> {tx});
            var expectedChainId = ChainHelpers.GetChainId(1);
            var txResult =
                await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "WithdrawRequest",
                    ChainHelpers.ConvertChainIdToBase58(expectedChainId));
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }
        
        [Fact]
        public async Task Withdraw_ChainCreation_ChainNotExist()
        {
            await Initialize(1000);
            ulong lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx = ContractTester.GenerateTransaction(CrossChainContractAddress, "RequestChainCreation", 
                sideChainInfo);
            await ContractTester.MineABlockAsync(new List<Transaction> {tx});
            var expectedChainId = ChainHelpers.GetChainId(2);
            var txResult =
                await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "WithdrawRequest",
                    ChainHelpers.ConvertChainIdToBase58(expectedChainId));
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }
        
        [Fact]
        public async Task Withdraw_ChainCreation_WrongStatus()
        {
            await Initialize(1000);
            ulong lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx = ContractTester.GenerateTransaction(CrossChainContractAddress, "RequestChainCreation", 
                sideChainInfo);
            await ContractTester.MineABlockAsync(new List<Transaction> {tx});
            var chainId = ChainHelpers.GetChainId(1);
            await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "CreateSideChain",
                    ChainHelpers.ConvertChainIdToBase58(chainId));
            var txResult =
                await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "WithdrawRequest",
                    ChainHelpers.ConvertChainIdToBase58(chainId));
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task Create_SideChain()
        {
            await Initialize(1000);
            ulong lockedTokenAmount = 10;
            await ApproveBalance(lockedTokenAmount);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx = ContractTester.GenerateTransaction(CrossChainContractAddress, "RequestChainCreation",
                sideChainInfo);
            await ContractTester.MineABlockAsync(new List<Transaction> {tx});
            var chainId = ChainHelpers.GetChainId(1);
            var txResult =
                await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "CreateSideChain",
                    ChainHelpers.ConvertChainIdToBase58(chainId));
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Mined);
        }
        
        [Fact]
        public async Task Create_SideChain_FireEvent()
        {
            ulong lockedTokenAmount = 10;           
            await Initialize(1000);
            await ApproveBalance(lockedTokenAmount);
            
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx = ContractTester.GenerateTransaction(CrossChainContractAddress, "RequestChainCreation",
                sideChainInfo);
            await ContractTester.MineABlockAsync(new List<Transaction> {tx});
            var txRes = await ContractTester.GetTransactionResult(tx.GetHash());
            var chainId = ChainHelpers.GetChainId(1);
            var txResult =
                await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "CreateSideChain",
                    ChainHelpers.ConvertChainIdToBase58(chainId));
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
            ulong lockedTokenAmount = 10;           
            await Initialize(1000);
            await ApproveBalance(lockedTokenAmount);
            
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress()
            };
            
            var tx = ContractTester.GenerateTransaction(CrossChainContractAddress, "RequestChainCreation",
                sideChainInfo);
            await ContractTester.MineABlockAsync(new List<Transaction> {tx});
            var chainId = ChainHelpers.GetChainId(2);
            var txResult =
                await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "CreateSideChain",
                    ChainHelpers.ConvertChainIdToBase58(chainId));
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task Request_SideChain_Disposal()
        {
            ulong lockedTokenAmount = 10;           
            await Initialize(1000);
            await ApproveBalance(lockedTokenAmount);
            
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx1 = ContractTester.GenerateTransaction(CrossChainContractAddress, "RequestChainCreation",
                sideChainInfo);
            await ContractTester.MineABlockAsync(new List<Transaction> {tx1});
            var chainId = ChainHelpers.GetChainId(1);
            var tx2 = ContractTester.GenerateTransaction(CrossChainContractAddress, "CreateSideChain",
                ChainHelpers.ConvertChainIdToBase58(chainId));
            await ContractTester.MineABlockAsync(new List<Transaction> {tx2});
            var txResult =
                await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "RequestChainDisposal",
                    ChainHelpers.ConvertChainIdToBase58(chainId));
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Mined);
        }
        
        [Fact]
        public async Task Dispose_SideChain()
        {
            ulong lockedTokenAmount = 10;           
            await Initialize(1000);
            await ApproveBalance(lockedTokenAmount);
            
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx1 = ContractTester.GenerateTransaction(CrossChainContractAddress, "RequestChainCreation",
                sideChainInfo);
            await ContractTester.MineABlockAsync(new List<Transaction> {tx1});
            var chainId = ChainHelpers.GetChainId(1);
            var tx2 = ContractTester.GenerateTransaction(CrossChainContractAddress, "CreateSideChain",
                ChainHelpers.ConvertChainIdToBase58(chainId));
            await ContractTester.MineABlockAsync(new List<Transaction> {tx2});
            var txResult1 =
                await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "RequestChainDisposal",
                    ChainHelpers.ConvertChainIdToBase58(chainId));
            var txResult2 =
                await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "DisposeSideChain",
                    ChainHelpers.ConvertChainIdToBase58(chainId));
            object[] data = ParamsPacker.Unpack(txResult2.Logs.First().Data.ToByteArray(),
                new[] {typeof(int)});
            var disposedChainId = (int) data[0];
            Assert.True(chainId == disposedChainId);
        }

        [Fact]
        public async Task GetChainStatus_Review()
        {
            ulong lockedTokenAmount = 10;           
            await Initialize(1000);
            await ApproveBalance(lockedTokenAmount);
            
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx1 = ContractTester.GenerateTransaction(CrossChainContractAddress, "RequestChainCreation",
                sideChainInfo);
            await ContractTester.MineABlockAsync(new List<Transaction> {tx1});
            var chainId = ChainHelpers.GetChainId(1);
            
            var status = await ContractTester.CallContractMethodAsync(CrossChainContractAddress, "GetChainStatus",
                ChainHelpers.ConvertChainIdToBase58(chainId));
            Assert.True(status.DeserializeToInt32() == 1);
        }
        
        [Fact]
        public async Task GetChainStatus_Active()
        {
            ulong lockedTokenAmount = 10;           
            await Initialize(1000);
            await ApproveBalance(lockedTokenAmount);
            
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx1 = ContractTester.GenerateTransaction(CrossChainContractAddress, "RequestChainCreation",
                sideChainInfo);
            await ContractTester.MineABlockAsync(new List<Transaction> {tx1});
            var chainId = ChainHelpers.GetChainId(1);
            await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "CreateSideChain",
                    ChainHelpers.ConvertChainIdToBase58(chainId));
            var status = await ContractTester.CallContractMethodAsync(CrossChainContractAddress, "GetChainStatus",
                ChainHelpers.ConvertChainIdToBase58(chainId));
            Assert.True(status.DeserializeToInt32() == 2);
        }
        
        [Fact]
        public async Task GetChainStatus_ChainNotExist()
        {
            ulong lockedTokenAmount = 10;           
            await Initialize(1000);
            await ApproveBalance(lockedTokenAmount);
            
            var chainId = ChainHelpers.GetChainId(1);
            var status = await ContractTester.CallContractMethodAsync(CrossChainContractAddress, "GetChainStatus",
                ChainHelpers.ConvertChainIdToBase58(chainId));
            Assert.Empty(status);
        }

        [Fact]
        public async Task Get_SideChain_Height()
        {
            ulong lockedTokenAmount = 10;           
            await Initialize(1000);
            await ApproveBalance(lockedTokenAmount);
            
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };
            
            var tx1 = ContractTester.GenerateTransaction(CrossChainContractAddress, "RequestChainCreation",
                sideChainInfo);
            await ContractTester.MineABlockAsync(new List<Transaction> {tx1});
            var chainId = ChainHelpers.GetChainId(1);
            await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "CreateSideChain",
                ChainHelpers.ConvertChainIdToBase58(chainId));
            var height = await ContractTester.CallContractMethodAsync(CrossChainContractAddress, "GetSideChainHeight",
                ChainHelpers.ConvertChainIdToBase58(chainId));
            Assert.True(height.DeserializeToInt32() == 0);
        }
        
        [Fact]
        public async Task Get_SideChain_Height_ChainNotExist()
        {
            ulong lockedTokenAmount = 10;           
            await Initialize(1000);
            await ApproveBalance(lockedTokenAmount);
            var chainId = ChainHelpers.GetChainId(1);
            var height = await ContractTester.CallContractMethodAsync(CrossChainContractAddress, "GetSideChainHeight",
                ChainHelpers.ConvertChainIdToBase58(chainId));
            Assert.Empty(height);
        }
    }
}