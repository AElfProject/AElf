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
     * Lock token and resource is disable.
     * Recharge is disable.
    */
    public class SideChainLifeTimeManagementTest : CrossChainContractTestBase
    {
        [Fact]
        public async Task Request_SideChain_Creation()
        {
            var initResult = await ContractTester.ExecuteContractWithMiningAsync(TokenContractAddress, "Initialize",
                "ELF", "elf token", 1000_000UL, 2U);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress()
            };
            var txResult = await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "RequestChainCreation",
                sideChainInfo);
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Mined);
            Assert.False(txResult.RetVal.ToByteArray().IsNullOrEmpty());
            var expectedChainId = ChainHelpers.ConvertChainIdToBase58(ChainHelpers.GetChainId(1));
            var actualChainId = txResult.RetVal.ToStringUtf8();
            Assert.Equal(expectedChainId, actualChainId);
        }

        [Fact]
        public async Task Request_SideChain_Creation_WrongProposer()
        {
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
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
            
            var txResult = await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "RequestChainCreation",
                sideChainInfo);
            var expectedChainId = ChainHelpers.ConvertChainIdToBase58(ChainHelpers.GetChainId(2));
            var actualChainId = txResult.RetVal.ToStringUtf8();
            Assert.Equal(expectedChainId, actualChainId);
        }


        [Fact]
        public async Task Withdraw_ChainCreation()
        {
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
            var ecKeyPair = CryptoHelpers.GenerateKeyPair();
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = Address.FromPublicKey(ecKeyPair.PublicKey)
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
            var expectedChainId = ChainHelpers.GetChainId(2);
            var txResult =
                await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "WithdrawRequest",
                    ChainHelpers.ConvertChainIdToBase58(expectedChainId));
            var status = txResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
        }
        
//        [Fact]
//        public async Task Withdraw_ChainCreation_WrongStatus()
//        {
//            var sideChainInfo = new SideChainInfo
//            {
//                SideChainStatus = SideChainStatus.Apply,
//                ContractCode = ByteString.Empty,
//                IndexingPrice = 1,
//                Proposer = CrossChainContractTestHelper.GetAddress()
//            };
//            
//            var tx = ContractTester.GenerateTransaction(CrossChainContractAddress, "RequestChainCreation", 
//                sideChainInfo);
//            await ContractTester.MineABlockAsync(new List<Transaction> {tx});
//            var expectedChainId = ChainHelpers.GetChainId(2);
//            var txResult =
//                await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "WithdrawRequest",
//                    ChainHelpers.ConvertChainIdToBase58(expectedChainId));
//            var status = txResult.Status;
//            Assert.True(status == TransactionResultStatus.Failed);
//        }

        [Fact]
        public async Task Create_SideChain()
        {
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
            var chainId = ChainHelpers.GetChainId(1);
            var txResult =
                await ContractTester.ExecuteContractWithMiningAsync(CrossChainContractAddress, "CreateSideChain",
                    ChainHelpers.ConvertChainIdToBase58(chainId));
            Assert.True(txResult.Logs.Count > 0);
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
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress()
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
        
    }
}