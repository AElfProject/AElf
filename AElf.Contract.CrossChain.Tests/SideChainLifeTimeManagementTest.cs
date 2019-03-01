using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AElf.Common;
using AElf.CrossChain;
using AElf.Cryptography;
using AElf.Kernel;
using Google.Protobuf;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AElf.Contract.CrossChain.Tests
{
    /*
     * Todo:
     * Side chain creation proposal is disable.
     * Lock token and resource is disable. 
    */
    public class SideChainLifeTimeManagementTest : CrossChainContractTestBase
    {
        [Fact]
        public async Task Request_SideChain_Creation()
        {
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
            var chainIdString = Encoding.UTF8.GetString(txResult.RetVal.ToByteArray());
            var actualChainId = JObject.Parse(chainIdString)["chain_id"];
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
            var chainIdString = Encoding.UTF8.GetString(txResult.RetVal.ToByteArray());
            var actualChainId = JObject.Parse(chainIdString)["chain_id"];
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
    }
}