using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs6;
using AElf.Contracts.Deployer;
using AElf.Contracts.TestContract.CommitmentScheme;
using AElf.Contracts.TestKit;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.AEDPoSExtension.Demo.Tests
{
    public class RandomNumberGenerateTests : AEDPoSExtensionDemoTestBase
    {
        [Fact]
        public async Task RequestRandomNumberTest()
        {
            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();
            var requestTx = ConsensusStub.RequestRandomNumber.GetTransaction(new Hash());
            var refBlockNumber = requestTx.RefBlockNumber;
            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                requestTx
            });
            var requestTrace = TransactionTraceProvider.GetTransactionTrace(requestTx.GetHash());
            var randomNumberOrder = new RandomNumberOrder();
            randomNumberOrder.MergeFrom(requestTrace.ReturnValue);
            var targetBlockNumber = randomNumberOrder.BlockHeight;

            targetBlockNumber.ShouldBeLessThan(refBlockNumber + 100);
        }

        internal CommitmentSchemeContractContainer.CommitmentSchemeContractStub CommitmentSchemeStub;
        private Hash Secret => HashHelper.ComputeFrom("Secret");
        
        [Fact]
        public async Task<Hash> RequestRandomNumberWithCommitmentSchemeTest()
        {
            await BlockMiningService.MineBlockToNextRoundAsync();
            var dict = await DeployCommitmentSchemeContract();
            CommitmentSchemeStub =
                GetTester<CommitmentSchemeContractContainer.CommitmentSchemeContractStub>(dict.First().Value,
                    Accounts[0].KeyPair);

            var commitment = HashHelper.ComputeFrom(Secret);
            var requestTx = CommitmentSchemeStub.RequestRandomNumber.GetTransaction(commitment);
            await BlockMiningService.MineBlockAsync(new List<Transaction> {requestTx});
            var requestTrace = TransactionTraceProvider.GetTransactionTrace(requestTx.GetHash());
            var randomNumberOrder = new RandomNumberOrder();
            randomNumberOrder.MergeFrom(requestTrace.ReturnValue);
            randomNumberOrder.BlockHeight.ShouldBePositive();
            randomNumberOrder.TokenHash.ShouldBe(commitment);

            return requestTx.GetHash();
        }

        [Fact]
        public async Task GetRandomNumberWithCommitmentSchemeTest()
        {
            var txHash = await RequestRandomNumberWithCommitmentSchemeTest();
            var requestTrace = TransactionTraceProvider.GetTransactionTrace(txHash);
            var randomNumberOrder = new RandomNumberOrder();
            randomNumberOrder.MergeFrom(requestTrace.ReturnValue);
            await BlockMiningService.MineBlockAsync(randomNumberOrder.BlockHeight);
            var getTx = CommitmentSchemeStub.GetRandomNumber.GetTransaction(Secret);
            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                getTx
            });
            var getTrace = TransactionTraceProvider.GetTransactionTrace(getTx.GetHash());
            var randomNumber = new Hash();
            randomNumber.MergeFrom(getTrace.ReturnValue);
            randomNumber.Value.ShouldNotBeEmpty();
        }

        private async Task<Dictionary<Hash, Address>> DeployCommitmentSchemeContract()
        {
            return await BlockMiningService.DeploySystemContractsAsync(new Dictionary<Hash, byte[]>
            {
                {
                    CommitmentSchemeSmartContractAddressName,
                    ContractsDeployer.GetContractCodes<AEDPoSExtensionDemoModule>().Last().Value
                }
            }, false);
        }
    }
}