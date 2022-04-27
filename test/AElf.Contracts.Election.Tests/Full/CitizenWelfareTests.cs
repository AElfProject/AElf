using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Profit;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractTests
    {
        private readonly ProfitShare _profitShare = new ProfitShare();

        [Fact]
        public async Task CitizenWelfareTest()
        {
            foreach (var keyPair in CoreDataCenterKeyPairs)
            {
                await AnnounceElectionAsync(keyPair);
            }

            await Term1Async();

            await ProduceBlocks(BootMinerKeyPair, 10);
            await NextTerm(BootMinerKeyPair);

            await Term2Async();
            
            await ProduceBlocks(BootMinerKeyPair, 10);
            await NextTerm(BootMinerKeyPair);

            await Term3Async();
            await ProduceBlocks(BootMinerKeyPair, 10);
            await NextTerm(BootMinerKeyPair);

            await Term4Async();
            await ProduceBlocks(BootMinerKeyPair, 10);
            await NextTerm(BootMinerKeyPair);
            
            await ProduceBlocks(BootMinerKeyPair, 10);
            await NextTerm(BootMinerKeyPair);
            await ProduceBlocks(BootMinerKeyPair, 10);
            await NextTerm(BootMinerKeyPair);
            await ProduceBlocks(BootMinerKeyPair, 10);
            await NextTerm(BootMinerKeyPair);
            await ProduceBlocks(BootMinerKeyPair, 10);
            await NextTerm(BootMinerKeyPair);

            await Term9Async();
        }

        /// <summary>
        /// Voter1 votes: (50, 10)
        /// Vote1 change option: true
        /// Voter2 votes: (50, 10)
        /// Voter2 change optioin: false
        /// </summary>
        private async Task Term1Async()
        {
            await VoteToCandidateAsync(VoterKeyPairs[0], CoreDataCenterKeyPairs[0].PublicKey.ToHex(), 50 * 86400,
                10_00000000);

            {
                var profitDetail =
                    await GetCitizenWelfareProfitDetails(Address.FromPublicKey(VoterKeyPairs[0].PublicKey));
                profitDetail.Details[0].Id.ShouldNotBeNull();
                profitDetail.Details[0].StartPeriod.ShouldBe(2);
                profitDetail.Details[0].EndPeriod.ShouldBe(8);
                _profitShare.AddShares(2, 8, VoterKeyPairs[0].PublicKey.ToHex(), profitDetail.Details[0].Shares);
            }

            {
                var electorVotes = await ElectionContractStub.GetElectorVote.CallAsync(new StringValue
                {
                    Value = VoterKeyPairs[0].PublicKey.ToHex()
                });
                await ChangeVotingOption(VoterKeyPairs[0], CoreDataCenterKeyPairs[1].PublicKey.ToHex(),
                    electorVotes.ActiveVotingRecordIds.First(), true);
            }

            {
                var profitDetail =
                    await GetCitizenWelfareProfitDetails(Address.FromPublicKey(VoterKeyPairs[0].PublicKey));
                profitDetail.Details[0].Id.ShouldNotBeNull();
                profitDetail.Details[0].StartPeriod.ShouldBe(2);
                profitDetail.Details[0].EndPeriod.ShouldBe(8);
            }

            await VoteToCandidateAsync(VoterKeyPairs[1], CoreDataCenterKeyPairs[1].PublicKey.ToHex(), 50 * 86400,
                10_00000000);

            {
                var profitDetail =
                    await GetCitizenWelfareProfitDetails(Address.FromPublicKey(VoterKeyPairs[1].PublicKey));
                profitDetail.Details[0].Id.ShouldNotBeNull();
                profitDetail.Details[0].StartPeriod.ShouldBe(2);
                profitDetail.Details[0].EndPeriod.ShouldBe(8);
                _profitShare.AddShares(2, 8, VoterKeyPairs[1].PublicKey.ToHex(), profitDetail.Details[0].Shares);
            }
            
            {
                var electorVotes = await ElectionContractStub.GetElectorVote.CallAsync(new StringValue
                {
                    Value = VoterKeyPairs[1].PublicKey.ToHex()
                });
                await ChangeVotingOption(VoterKeyPairs[1], CoreDataCenterKeyPairs[0].PublicKey.ToHex(),
                    electorVotes.ActiveVotingRecordIds.First(), false);
            }

            {
                var profitDetail =
                    await GetCitizenWelfareProfitDetails(Address.FromPublicKey(VoterKeyPairs[1].PublicKey));
                profitDetail.Details[0].Id.ShouldNotBeNull();
                profitDetail.Details[0].StartPeriod.ShouldBe(2);
                profitDetail.Details[0].EndPeriod.ShouldBe(8);
            }
        }

        /// <summary>
        /// Voter3 votes: (50, 10)
        /// Voter4 votes: (50, 10)
        /// Voter5 votes: (50, 10)
        /// Voter6 votes: (50, 10)
        /// </summary>
        private async Task Term2Async()
        {
            await VoteToCandidateAsync(VoterKeyPairs[2], CoreDataCenterKeyPairs[2].PublicKey.ToHex(), 50 * 86400,
                10_00000000);

            {
                var profitDetail =
                    await GetCitizenWelfareProfitDetails(Address.FromPublicKey(VoterKeyPairs[2].PublicKey));

                profitDetail.Details[0].Id.ShouldNotBeNull();
                profitDetail.Details[0].StartPeriod.ShouldBe(3);
                profitDetail.Details[0].EndPeriod.ShouldBe(9);
                _profitShare.AddShares(3, 9, CoreDataCenterKeyPairs[2].PublicKey.ToHex(),
                    profitDetail.Details[0].Shares);
            }
            
            await VoteToCandidateAsync(VoterKeyPairs[3], CoreDataCenterKeyPairs[3].PublicKey.ToHex(), 50 * 86400,
                10_00000000);

            {
                var profitDetail =
                    await GetCitizenWelfareProfitDetails(Address.FromPublicKey(VoterKeyPairs[3].PublicKey));

                profitDetail.Details[0].Id.ShouldNotBeNull();
                profitDetail.Details[0].StartPeriod.ShouldBe(3);
                profitDetail.Details[0].EndPeriod.ShouldBe(9);
                _profitShare.AddShares(3, 9, CoreDataCenterKeyPairs[3].PublicKey.ToHex(),
                    profitDetail.Details[0].Shares);
            }
            
            await VoteToCandidateAsync(VoterKeyPairs[4], CoreDataCenterKeyPairs[4].PublicKey.ToHex(), 50 * 86400,
                10_00000000);

            {
                var profitDetail =
                    await GetCitizenWelfareProfitDetails(Address.FromPublicKey(VoterKeyPairs[4].PublicKey));

                profitDetail.Details[0].Id.ShouldNotBeNull();
                profitDetail.Details[0].StartPeriod.ShouldBe(3);
                profitDetail.Details[0].EndPeriod.ShouldBe(9);
                _profitShare.AddShares(3, 9, CoreDataCenterKeyPairs[4].PublicKey.ToHex(),
                    profitDetail.Details[0].Shares);
            }
            
            await VoteToCandidateAsync(VoterKeyPairs[5], CoreDataCenterKeyPairs[5].PublicKey.ToHex(), 50 * 86400,
                10_00000000);

            {
                var profitDetail =
                    await GetCitizenWelfareProfitDetails(Address.FromPublicKey(VoterKeyPairs[5].PublicKey));

                profitDetail.Details[0].Id.ShouldNotBeNull();
                profitDetail.Details[0].StartPeriod.ShouldBe(3);
                profitDetail.Details[0].EndPeriod.ShouldBe(9);
                _profitShare.AddShares(3, 9, CoreDataCenterKeyPairs[5].PublicKey.ToHex(),
                    profitDetail.Details[0].Shares);
            }
        }

        /// <summary>
        /// Voter2 votes: (20, 10)
        /// Voter3 change option: true
        /// Voter4 change option: false
        /// </summary>
        private async Task Term3Async()
        {
            await VoteToCandidateAsync(VoterKeyPairs[1], CoreDataCenterKeyPairs[1].PublicKey.ToHex(), 20 * 86400,
                10_00000000);

            {
                var profitDetail =
                    await GetCitizenWelfareProfitDetails(Address.FromPublicKey(VoterKeyPairs[1].PublicKey));

                profitDetail.Details[1].Id.ShouldNotBeNull();
                profitDetail.Details[1].StartPeriod.ShouldBe(4);
                profitDetail.Details[1].EndPeriod.ShouldBe(5);
                _profitShare.AddShares(4, 5, CoreDataCenterKeyPairs[1].PublicKey.ToHex(),
                    profitDetail.Details[1].Shares);
            }

            {
                var electorVotes = await ElectionContractStub.GetElectorVote.CallAsync(new StringValue
                {
                    Value = VoterKeyPairs[2].PublicKey.ToHex()
                });
                await ChangeVotingOption(VoterKeyPairs[2], CoreDataCenterKeyPairs[2].PublicKey.ToHex(),
                    electorVotes.ActiveVotingRecordIds.First(), true);
            }

            {
                var profitDetail =
                    await GetCitizenWelfareProfitDetails(Address.FromPublicKey(VoterKeyPairs[2].PublicKey));
                profitDetail.Details[0].Id.ShouldNotBeNull();
                profitDetail.Details[0].StartPeriod.ShouldBe(3);
                profitDetail.Details[0].EndPeriod.ShouldBe(10);
            }
            
            {
                var electorVotes = await ElectionContractStub.GetElectorVote.CallAsync(new StringValue
                {
                    Value = VoterKeyPairs[3].PublicKey.ToHex()
                });
                await ChangeVotingOption(VoterKeyPairs[3], CoreDataCenterKeyPairs[3].PublicKey.ToHex(),
                    electorVotes.ActiveVotingRecordIds.First(), false);
            }

            {
                var profitDetail =
                    await GetCitizenWelfareProfitDetails(Address.FromPublicKey(VoterKeyPairs[3].PublicKey));
                profitDetail.Details[0].Id.ShouldNotBeNull();
                profitDetail.Details[0].StartPeriod.ShouldBe(3);
                profitDetail.Details[0].EndPeriod.ShouldBe(9);
            }
        }

        /// <summary>
        /// Voter2 change option: true
        /// </summary>
        private async Task Term4Async()
        {
            {
                var electorVotes = await ElectionContractStub.GetElectorVote.CallAsync(new StringValue
                {
                    Value = VoterKeyPairs[1].PublicKey.ToHex()
                });
                await ChangeVotingOption(VoterKeyPairs[1], CoreDataCenterKeyPairs[2].PublicKey.ToHex(),
                    electorVotes.ActiveVotingRecordIds.Last(), true);
            }

            {
                var profitDetail =
                    await GetCitizenWelfareProfitDetails(Address.FromPublicKey(VoterKeyPairs[1].PublicKey));
                profitDetail.Details[1].Id.ShouldNotBeNull();
                profitDetail.Details[1].StartPeriod.ShouldBe(4);
                profitDetail.Details[1].EndPeriod.ShouldBe(6);
            }
        }
        
        /// <summary>
        /// Voter5 change option: true
        /// Voter6 change option: false
        /// </summary>
        private async Task Term9Async()
        {
            {
                var electorVotes = await ElectionContractStub.GetElectorVote.CallAsync(new StringValue
                {
                    Value = VoterKeyPairs[4].PublicKey.ToHex()
                });
                await ChangeVotingOption(VoterKeyPairs[4], CoreDataCenterKeyPairs[5].PublicKey.ToHex(),
                    electorVotes.ActiveVotingRecordIds.Last(), true);
            }

            {
                var profitDetail =
                    await GetCitizenWelfareProfitDetails(Address.FromPublicKey(VoterKeyPairs[4].PublicKey));
                profitDetail.Details[0].Id.ShouldNotBeNull();
                profitDetail.Details[0].StartPeriod.ShouldBe(3);
                profitDetail.Details[0].EndPeriod.ShouldBe(16);
            }

            {
                var electorVotes = await ElectionContractStub.GetElectorVote.CallAsync(new StringValue
                {
                    Value = VoterKeyPairs[5].PublicKey.ToHex()
                });
                await ChangeVotingOption(VoterKeyPairs[5], CoreDataCenterKeyPairs[4].PublicKey.ToHex(),
                    electorVotes.ActiveVotingRecordIds.Last(), false);
            }

            {
                var profitDetail =
                    await GetCitizenWelfareProfitDetails(Address.FromPublicKey(VoterKeyPairs[5].PublicKey));
                profitDetail.Details[0].Id.ShouldNotBeNull();
                profitDetail.Details[0].StartPeriod.ShouldBe(3);
                profitDetail.Details[0].EndPeriod.ShouldBe(9);
            }
        }

        private async Task<ProfitDetails> GetCitizenWelfareProfitDetails(Address voterAddress)
        {
            return await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = ProfitSchemeIdList[ProfitType.CitizenWelfare],
                Beneficiary = voterAddress
            });
        }
    }
}