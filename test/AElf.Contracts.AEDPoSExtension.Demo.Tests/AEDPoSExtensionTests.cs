using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs6;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.AEDPoSExtension.Demo.Tests
{
    // ReSharper disable once InconsistentNaming
    public class AEDPoSExtensionTests : AEDPoSExtensionDemoTestBase
    {
        [Fact]
        public async Task Demo_Test()
        {
            // Check round information after initialization.
            {
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.RoundNumber.ShouldBe(1);
                round.TermNumber.ShouldBe(1);
                round.RealTimeMinersInformation.Count.ShouldBe(AEDPoSExtensionConstants.InitialKeyPairCount);

                var firstMinerPubkey = round.RealTimeMinersInformation.Single(m => m.Value.Order == 1).Key;
                var currentMinerPubkey = await ConsensusStub.GetCurrentMinerPubkey.CallAsync(new Empty());
                currentMinerPubkey.Value.ShouldBe(firstMinerPubkey);
                (await ConsensusStub.IsCurrentMiner.CallAsync(
                        Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(firstMinerPubkey)))).Value
                    .ShouldBeTrue();
            }

            // We can use this method process testing.
            // Basically this will produce one block with no transaction.
            await BlockMiningService.MineBlockAsync();

            // And this will produce one block with one transaction.
            // This transaction will call Create method of Token Contract.
            var createTokenTransaction = TokenStub.Create.GetTransaction(new CreateInput
            {
                Symbol = "ELF",
                Decimals = 8,
                TokenName = "Test",
                Issuer = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[0].PublicKey),
                IsBurnable = true,
                TotalSupply = 1_000_000_000_00000000
            });
            const long issueTokenAmount = 10_0000_00000000;
            var issueToAddress = Address.FromPublicKey(MissionedECKeyPairs.InitialKeyPairs.First().PublicKey);
            var issueTokenTransaction = TokenStub.Issue.GetTransaction(new IssueInput
            {
                Symbol = "ELF",
                Amount = issueTokenAmount,
                To = issueToAddress
            });
            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                createTokenTransaction,
                issueTokenTransaction
            });

            var createTokenTransactionTrace =
                TransactionTraceProvider.GetTransactionTrace(createTokenTransaction.GetHash());
            createTokenTransactionTrace.ExecutionStatus.ShouldBe(ExecutionStatus.Executed);

            // Check whether previous Create transaction successfully executed.
            {
                var tokenInfo = await TokenStub.GetTokenInfo.CallAsync(new GetTokenInfoInput {Symbol = "ELF"});
                tokenInfo.Symbol.ShouldBe("ELF");
            }

            for (var i = 0; i < AEDPoSExtensionConstants.TinyBlocksNumber - 2; i++)
            {
                await BlockMiningService.MineBlockAsync();
            }
            
            // Check miner information
            {
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                var secondMinerPubkey = round.RealTimeMinersInformation.Single(m => m.Value.Order == 2).Key;
                var currentMinerPubkey = await ConsensusStub.GetCurrentMinerPubkey.CallAsync(new Empty());
                currentMinerPubkey.Value.ShouldBe(secondMinerPubkey);
                (await ConsensusStub.IsCurrentMiner.CallAsync(
                        Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(secondMinerPubkey)))).Value
                    .ShouldBeTrue();
            }

            var getBalanceTransaction = TokenStub.GetBalance.GetTransaction(new GetBalanceInput
            {
                Owner = issueToAddress,
                Symbol = "ELF"
            });
            // Miner of order 2 produce his first block.
            await BlockMiningService.MineBlockAsync(new List<Transaction> {getBalanceTransaction});

            var getBalanceTransactionTrace =
                TransactionTraceProvider.GetTransactionTrace(getBalanceTransaction.GetHash());
            getBalanceTransactionTrace.ReadableReturnValue.ShouldNotBeNull();
            getBalanceTransactionTrace.ReadableReturnValue.ShouldContain("balance");
            getBalanceTransactionTrace.ReadableReturnValue.ShouldContain(issueTokenAmount.ToString());

            // Next steps will check whether the AEDPoS process is correct.
            // Now 2 miners produced block during first round, so there should be 2 miners' OutValue isn't null.
            {
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.RealTimeMinersInformation.Values.Count(m => m.OutValue != null).ShouldBe(2);
            }

            for (var i = 0; i < AEDPoSExtensionConstants.TinyBlocksNumber - 1; i++)
            {
                await BlockMiningService.MineBlockAsync();
            }

            // Miner of order 3 produce his first block.
            await BlockMiningService.MineBlockAsync();

            {
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.RealTimeMinersInformation.Values.Count(m => m.OutValue != null).ShouldBe(3);
            }

            for (var i = 0; i < AEDPoSExtensionConstants.TinyBlocksNumber - 1; i++)
            {
                await BlockMiningService.MineBlockAsync();
            }

            // Currently we have 5 miners, and before this line, 3 miners already produced blocks.
            // 3 more blocks will end current round.
            for (var i = 0; i < AEDPoSExtensionConstants.TinyBlocksNumber * 3; i++)
            {
                await BlockMiningService.MineBlockAsync(new List<Transaction>());
            }

            // Check round number.
            {
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.RoundNumber.ShouldBe(2);
            }

            // 6 more blocks will end second round.
            for (var i = 0; i < AEDPoSExtensionConstants.TinyBlocksNumber * 6; i++)
            {
                await BlockMiningService.MineBlockAsync(new List<Transaction>());
            }

            // Check round number.
            {
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.RoundNumber.ShouldBeGreaterThanOrEqualTo(3);
            }
        }
        
        [Fact(Skip = "Redo this later.")]
        public async Task<Hash> RequestRandomNumber_Test()
        {
            const long minimumBlockHeight = 40;

            var transaction = ConsensusStub.RequestRandomNumber.GetTransaction(new RequestRandomNumberInput
            {
                MinimumBlockHeight = minimumBlockHeight
            });
            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                transaction
            });

            // TODO: Need to query result of transaction, this feature implemented in another PR.
            return Hash.Empty;
        }

        [Fact(Skip = "Redo this later.")]
        public async Task GetRandomNumber_WithNotEnoughParticipators_Test()
        {
            var hash = await RequestRandomNumber_Test();

            // Can't get random number.
            {
                var randomHash = await ConsensusStub.GetRandomNumber.CallAsync(hash);
                randomHash.Value.Count().ShouldBe(0);
            }

            BlockMiningService.SkipTime(4);

            await BlockMiningService.MineBlockAsync(40);

            // Can't get random number.
            {
                var randomHash = await ConsensusStub.GetRandomNumber.CallAsync(hash);
                randomHash.Value.Count().ShouldBe(0);
            }
        }
    }
}