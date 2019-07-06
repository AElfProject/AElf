using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractTests : ElectionContractTestBase
    {
        [Fact]
        public async Task UserVote_And_GetProfitAmount()
        {
            ValidationDataCenterKeyPairs.ForEach(async kp => await AnnounceElectionAsync(kp));

            var candidates = await ElectionContractStub.GetCandidates.CallAsync(new Empty());
            candidates.Value.Count.ShouldBe(ValidationDataCenterKeyPairs.Count);

            var moreVotesCandidates = ValidationDataCenterKeyPairs
                .Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
            moreVotesCandidates.ForEach(async kp =>
                await VoteToCandidate(VoterKeyPairs[0], kp.PublicKey.ToHex(), 100 * 86400, 2));

            {
                var votedCandidates = await ElectionContractStub.GetVotedCandidates.CallAsync(new Empty());
                votedCandidates.Value.Count.ShouldBe(EconomicContractsTestConstants.InitialCoreDataCenterCount);
            }

            var lessVotesCandidates = ValidationDataCenterKeyPairs
                .Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount)
                .Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
            lessVotesCandidates.ForEach(async kp =>
                await VoteToCandidate(VoterKeyPairs[0], kp.PublicKey.ToHex(), 100 * 86400, 1));

            {
                var votedCandidates = await ElectionContractStub.GetVotedCandidates.CallAsync(new Empty());
                votedCandidates.Value.Count.ShouldBe(EconomicContractsTestConstants.InitialCoreDataCenterCount * 2);
            }

            {
                var round = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.TermNumber.ShouldBe(1);
            }

            await ProduceBlocks(BootMinerKeyPair, 10);
            await NextTerm(BootMinerKeyPair);

            {
                var round = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.TermNumber.ShouldBe(2);
            }

            await ProduceBlocks(ValidationDataCenterKeyPairs[0], 10);
            await NextTerm(ValidationDataCenterKeyPairs[0]);

            {
                var round = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.TermNumber.ShouldBe(3);
            }

            var profitTester = GetProfitContractTester(VoterKeyPairs[0]);
            var profitBalance = (await profitTester.GetProfitAmount.CallAsync(new ProfitInput
            {
                ProfitId = ProfitItemsIds[ProfitType.CitizenWelfare],
                Symbol = "ELF"
            })).Value;
            profitBalance.ShouldBe(25000000);
        }

        [Fact]
        public async Task UserVote_CheckAllProfits()
        {
            const long txFee = 1_00000000L;

            //Preparation
            {
                ValidationDataCenterKeyPairs.ForEach(async kp => await AnnounceElectionAsync(kp));

                var candidates = await ElectionContractStub.GetCandidates.CallAsync(new Empty());
                candidates.Value.Count.ShouldBe(ValidationDataCenterKeyPairs.Count);

                var moreVotesCandidates = ValidationDataCenterKeyPairs
                    .Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
                moreVotesCandidates.ForEach(async kp =>
                    await VoteToCandidate(VoterKeyPairs[0], kp.PublicKey.ToHex(), 100 * 86400, 2));

                var lessVotesCandidates = ValidationDataCenterKeyPairs
                    .Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount)
                    .Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
                lessVotesCandidates.ForEach(async kp =>
                    await VoteToCandidate(VoterKeyPairs[0], kp.PublicKey.ToHex(), 100 * 86400, 1));

                var votedCandidates = await ElectionContractStub.GetVotedCandidates.CallAsync(new Empty());
                votedCandidates.Value.Count.ShouldBe(EconomicContractsTestConstants.InitialCoreDataCenterCount * 2);

                await ProduceBlocks(BootMinerKeyPair, 10);
                await NextTerm(BootMinerKeyPair);
                var round = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.TermNumber.ShouldBe(2);
            }

            //term 2 generate block
            {
                for (var i = 0; i < EconomicContractsTestConstants.InitialCoreDataCenterCount; i++)
                {
                    await ProduceBlocks(ValidationDataCenterKeyPairs[i], 10);
                }

                await NextTerm(InitialCoreDataCenterKeyPairs[0]);
                var round = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.TermNumber.ShouldBe(3);
            }

            //query and profit voter vote profit
            {
                var beforeToken = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = Address.FromPublicKey(VoterKeyPairs[0].PublicKey),
                    Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                })).Balance;

                var profitTester = GetProfitContractTester(VoterKeyPairs[0]);
                var profitBalance = (await profitTester.GetProfitAmount.CallAsync(new ProfitInput
                {
                    ProfitId = ProfitItemsIds[ProfitType.CitizenWelfare],
                    Symbol = "ELF"
                })).Value;
                profitBalance.ShouldBeGreaterThan(0);

                var profitResult = await profitTester.Profit.SendAsync(new ProfitInput
                {
                    ProfitId = ProfitItemsIds[ProfitType.CitizenWelfare],
                    Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                });
                profitResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var afterToken = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = Address.FromPublicKey(VoterKeyPairs[0].PublicKey),
                    Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                })).Balance;
                afterToken.ShouldBe(beforeToken + profitBalance - txFee);
            }

            for (var i = 0; i < EconomicContractsTestConstants.InitialCoreDataCenterCount; i++)
            {
                await ProduceBlocks(ValidationDataCenterKeyPairs[i], 10);
            }

            await NextTerm(InitialCoreDataCenterKeyPairs[0]);

            //query and profit miner profit
            {
                foreach (var miner in ValidationDataCenterKeyPairs.Take(EconomicContractsTestConstants
                    .InitialCoreDataCenterCount))
                {
                    var beforeToken = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(miner.PublicKey),
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    })).Balance;

                    var profitTester = GetProfitContractTester(miner);

                    //basic weight - 40%
                    var basicMinerRewardAmount = (await profitTester.GetProfitAmount.CallAsync(new ProfitInput
                    {
                        ProfitId = ProfitItemsIds[ProfitType.BasicMinerReward],
                        Symbol = "ELF"
                    })).Value;
                    basicMinerRewardAmount.ShouldBeGreaterThan(0);

                    //vote weight - 10%
                    var votesWeightRewardAmount = (await profitTester.GetProfitAmount.CallAsync(new ProfitInput
                    {
                        ProfitId = ProfitItemsIds[ProfitType.VotesWeightReward],
                        Symbol = "ELF"
                    })).Value;
                    votesWeightRewardAmount.ShouldBeGreaterThan(0);

                    //re-election weight - 10%
                    var reElectionBalance = (await profitTester.GetProfitAmount.CallAsync(new ProfitInput
                    {
                        ProfitId = ProfitItemsIds[ProfitType.ReElectionReward],
                        Symbol = "ELF"
                    })).Value;
                    reElectionBalance.ShouldBeGreaterThan(0);

                    //backup weight - 20%
                    var backupBalance = (await profitTester.GetProfitAmount.CallAsync(new ProfitInput
                    {
                        ProfitId = ProfitItemsIds[ProfitType.BackupSubsidy],
                        Symbol = "ELF"
                    })).Value;
                    backupBalance.ShouldBeGreaterThan(0);

                    //Profit all
                    var profitBasicResult = await profitTester.Profit.SendAsync(new ProfitInput
                    {
                        ProfitId = ProfitItemsIds[ProfitType.BasicMinerReward],
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    });
                    profitBasicResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    var voteResult = await profitTester.Profit.SendAsync(new ProfitInput
                    {
                        ProfitId = ProfitItemsIds[ProfitType.VotesWeightReward],
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    });
                    voteResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    var reElectionResult = await profitTester.Profit.SendAsync(new ProfitInput
                    {
                        ProfitId = ProfitItemsIds[ProfitType.ReElectionReward],
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    });
                    reElectionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    var backupResult = await profitTester.Profit.SendAsync(new ProfitInput
                    {
                        ProfitId = ProfitItemsIds[ProfitType.BackupSubsidy],
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    });
                    backupResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    var afterToken = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(miner.PublicKey),
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    })).Balance;

                    afterToken.ShouldBe(beforeToken + basicMinerRewardAmount + votesWeightRewardAmount +
                                        reElectionBalance + backupBalance - txFee * 4);
                }
            }
            
            for (var i = 0; i < EconomicContractsTestConstants.InitialCoreDataCenterCount; i++)
            {
                await ProduceBlocks(ValidationDataCenterKeyPairs[i], 10);
            }

            await NextTerm(InitialCoreDataCenterKeyPairs[0]);
            
            //query and profit miner profit
            {
                foreach (var miner in ValidationDataCenterKeyPairs.Take(EconomicContractsTestConstants
                    .InitialCoreDataCenterCount))
                {
                    var beforeToken = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(miner.PublicKey),
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    })).Balance;

                    var profitTester = GetProfitContractTester(miner);

                    //basic weight - 40%
                    var basicMinerRewardAmount = (await profitTester.GetProfitAmount.CallAsync(new ProfitInput
                    {
                        ProfitId = ProfitItemsIds[ProfitType.BasicMinerReward],
                        Symbol = "ELF"
                    })).Value;
                    basicMinerRewardAmount.ShouldBeGreaterThan(0);

                    //vote weight - 10%
                    var votesWeightRewardAmount = (await profitTester.GetProfitAmount.CallAsync(new ProfitInput
                    {
                        ProfitId = ProfitItemsIds[ProfitType.VotesWeightReward],
                        Symbol = "ELF"
                    })).Value;
                    votesWeightRewardAmount.ShouldBeGreaterThan(0);

                    //re-election weight - 10%
                    var reElectionBalance = (await profitTester.GetProfitAmount.CallAsync(new ProfitInput
                    {
                        ProfitId = ProfitItemsIds[ProfitType.ReElectionReward],
                        Symbol = "ELF"
                    })).Value;
                    reElectionBalance.ShouldBeGreaterThan(0);

                    //backup weight - 20%
                    var backupBalance = (await profitTester.GetProfitAmount.CallAsync(new ProfitInput
                    {
                        ProfitId = ProfitItemsIds[ProfitType.BackupSubsidy],
                        Symbol = "ELF"
                    })).Value;
                    backupBalance.ShouldBeGreaterThan(0);

                    //Profit all
                    var profitBasicResult = await profitTester.Profit.SendAsync(new ProfitInput
                    {
                        ProfitId = ProfitItemsIds[ProfitType.BasicMinerReward],
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    });
                    profitBasicResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    var voteResult = await profitTester.Profit.SendAsync(new ProfitInput
                    {
                        ProfitId = ProfitItemsIds[ProfitType.VotesWeightReward],
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    });
                    voteResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    var reElectionResult = await profitTester.Profit.SendAsync(new ProfitInput
                    {
                        ProfitId = ProfitItemsIds[ProfitType.ReElectionReward],
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    });
                    reElectionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    var backupResult = await profitTester.Profit.SendAsync(new ProfitInput
                    {
                        ProfitId = ProfitItemsIds[ProfitType.BackupSubsidy],
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    });
                    backupResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    var afterToken = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(miner.PublicKey),
                        Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                    })).Balance;

                    afterToken.ShouldBe(beforeToken + basicMinerRewardAmount + votesWeightRewardAmount +
                                        reElectionBalance + backupBalance - txFee * 4);
                }
            }
        }
    }
}