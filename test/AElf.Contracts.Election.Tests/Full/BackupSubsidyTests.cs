using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Profit;
using AElf.Contracts.Treasury;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election;

public partial class ElectionContractTests
{
    [Fact]
    public async Task Announcement_And_QuitElection_Check_BackupSubsidy()
    {
        var announceElectionKeyPair = ValidationDataCenterKeyPairs.First();
        var candidateAdmin = ValidationDataCenterKeyPairs.Last();
        var candidateAdminAddress = Address.FromPublicKey(candidateAdmin.PublicKey);
        await AnnounceElectionAsync(announceElectionKeyPair, candidateAdminAddress);

        var candidateBackupShare =
            await GetBackupSubsidyProfitDetails(Address.FromPublicKey(announceElectionKeyPair.PublicKey));
        candidateBackupShare.Details.Count.ShouldBe(1);
        candidateBackupShare.Details.First().Shares.ShouldBe(1);
        candidateBackupShare.Details.First().EndPeriod.ShouldBe(long.MaxValue);
        candidateBackupShare.Details.First().IsWeightRemoved.ShouldBeFalse();

        await QuitElectionAsync(announceElectionKeyPair, candidateAdmin);
        candidateBackupShare =
            await GetBackupSubsidyProfitDetails(Address.FromPublicKey(announceElectionKeyPair.PublicKey));
        candidateBackupShare.Details.Count.ShouldBe(1);
        candidateBackupShare.Details.First().Shares.ShouldBe(1);
        candidateBackupShare.Details.First().IsWeightRemoved.ShouldBeTrue();
    }

    [Fact]
    public async Task Announcement_SetProfitsReceiver_Check_BackupSubsidy()
    {
        var announceElectionKeyPair = ValidationDataCenterKeyPairs.First();
        var candidateAdmin = ValidationDataCenterKeyPairs.Last();
        var profitReceiver = candidateAdmin;
        var candidateAdminAddress = Address.FromPublicKey(candidateAdmin.PublicKey);
        await AnnounceElectionAsync(announceElectionKeyPair, candidateAdminAddress);

        var candidateAdminStub =
            GetTester<TreasuryContractImplContainer.TreasuryContractImplStub>(TreasuryContractAddress,
                candidateAdmin);
        // First set profit receiver
        {
            await candidateAdminStub.SetProfitsReceiver.SendAsync(
                new Treasury.SetProfitsReceiverInput
                {
                    Pubkey = announceElectionKeyPair.PublicKey.ToHex(),
                    ProfitsReceiverAddress = Address.FromPublicKey(profitReceiver.PublicKey)
                });
            var getProfitReceiver = await GetProfitReceiver(announceElectionKeyPair.PublicKey.ToHex());
            getProfitReceiver.ShouldBe(Address.FromPublicKey(profitReceiver.PublicKey));
        }
        // Check backup subsidy
        {
            var candidateBackupShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(announceElectionKeyPair.PublicKey));
            candidateBackupShare.Details.First().IsWeightRemoved.ShouldBeTrue();

            var profitReceiverBackShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(profitReceiver.PublicKey));
            profitReceiverBackShare.Details.Count.ShouldBe(1);
            profitReceiverBackShare.Details.First().Shares.ShouldBe(1);
            profitReceiverBackShare.Details.First().IsWeightRemoved.ShouldBeFalse();
        }
        // Second set profit receiver
        {
            await candidateAdminStub.SetProfitsReceiver.SendAsync(
                new Treasury.SetProfitsReceiverInput
                {
                    Pubkey = announceElectionKeyPair.PublicKey.ToHex(),
                    ProfitsReceiverAddress = Address.FromPublicKey(announceElectionKeyPair.PublicKey)
                });
            var getProfitReceiver = await GetProfitReceiver(announceElectionKeyPair.PublicKey.ToHex());
            getProfitReceiver.ShouldBe(Address.FromPublicKey(announceElectionKeyPair.PublicKey));
        }
        // Check backup subsidy
        {
            var profitReceiverBackShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(profitReceiver.PublicKey));
            profitReceiverBackShare.Details.Count.ShouldBe(1);
            profitReceiverBackShare.Details.First().Shares.ShouldBe(1);
            profitReceiverBackShare.Details.First().IsWeightRemoved.ShouldBeTrue();

            var candidateBackupShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(announceElectionKeyPair.PublicKey));
            candidateBackupShare.Details.Count.ShouldBe(2);
            candidateBackupShare.Details.Last().Shares.ShouldBe(1);
            candidateBackupShare.Details.First().IsWeightRemoved.ShouldBeTrue();
            candidateBackupShare.Details.Last().IsWeightRemoved.ShouldBeFalse();
        }
    }

    [Fact]
    public async Task Announcement_SetProfitsReceiver_Same_Receiver_Check_BackupSubsidy()
    {
        var announceElectionKeyPair = ValidationDataCenterKeyPairs.First();
        var candidateAdmin = ValidationDataCenterKeyPairs.Last();
        var profitReceiver = candidateAdmin;
        var candidateAdminAddress = Address.FromPublicKey(candidateAdmin.PublicKey);
        await AnnounceElectionAsync(announceElectionKeyPair, candidateAdminAddress);

        var candidateAdminStub =
            GetTester<TreasuryContractImplContainer.TreasuryContractImplStub>(TreasuryContractAddress,
                candidateAdmin);
        // First set profit receiver
        {
            await candidateAdminStub.SetProfitsReceiver.SendAsync(
                new Treasury.SetProfitsReceiverInput
                {
                    Pubkey = announceElectionKeyPair.PublicKey.ToHex(),
                    ProfitsReceiverAddress = Address.FromPublicKey(profitReceiver.PublicKey)
                });
            var getProfitReceiver = await GetProfitReceiver(announceElectionKeyPair.PublicKey.ToHex());
            getProfitReceiver.ShouldBe(Address.FromPublicKey(profitReceiver.PublicKey));
        }
        // Check backup subsidy
        {
            var candidateBackupShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(announceElectionKeyPair.PublicKey));
            candidateBackupShare.Details.Count.ShouldBe(1);
            candidateBackupShare.Details.First().IsWeightRemoved.ShouldBeTrue();

            var profitReceiverBackShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(profitReceiver.PublicKey));
            profitReceiverBackShare.Details.Count.ShouldBe(1);
            profitReceiverBackShare.Details.First().Shares.ShouldBe(1);
            profitReceiverBackShare.Details.First().IsWeightRemoved.ShouldBeFalse();
        }
        // Second set profit receiver, same receiver
        {
            await candidateAdminStub.SetProfitsReceiver.SendAsync(
                new Treasury.SetProfitsReceiverInput
                {
                    Pubkey = announceElectionKeyPair.PublicKey.ToHex(),
                    ProfitsReceiverAddress = Address.FromPublicKey(profitReceiver.PublicKey)
                });
            var getProfitReceiver = await GetProfitReceiver(announceElectionKeyPair.PublicKey.ToHex());
            getProfitReceiver.ShouldBe(Address.FromPublicKey(profitReceiver.PublicKey));
        }
        // Check backup subsidy
        {
            var profitReceiverBackShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(profitReceiver.PublicKey));
            profitReceiverBackShare.Details.Count.ShouldBe(1);
            profitReceiverBackShare.Details.First().Shares.ShouldBe(1);
            profitReceiverBackShare.Details.First().IsWeightRemoved.ShouldBeFalse();
        }
    }

    [Fact]
    public async Task ReplaceCandidatePubkey_SetProfitsReceiver_Check_BackupSubsidy()
    {
        var announceElectionKeyPair = ValidationDataCenterKeyPairs.First();
        var candidateAdmin = ValidationDataCenterKeyPairs.Last();
        var newKeyPair = ValidationDataCenterKeyPairs.Skip(1).First();
        var profitReceiver = ValidationDataCenterKeyPairs.Skip(2).First();

        var candidateAdminAddress = Address.FromPublicKey(candidateAdmin.PublicKey);
        await AnnounceElectionAsync(announceElectionKeyPair, candidateAdminAddress);

        // Check profit receiver
        {
            var getProfitReceiver = await GetProfitReceiver(announceElectionKeyPair.PublicKey.ToHex());
            getProfitReceiver.ShouldBe(Address.FromPublicKey(announceElectionKeyPair.PublicKey));
        }
        // ReplaceCandidatePubkey
        {
            var candidateAdminStub =
                GetTester<ElectionContractImplContainer.ElectionContractImplStub>(ElectionContractAddress,
                    candidateAdmin);
            await candidateAdminStub.ReplaceCandidatePubkey.SendAsync(new ReplaceCandidatePubkeyInput
            {
                OldPubkey = announceElectionKeyPair.PublicKey.ToHex(),
                NewPubkey = newKeyPair.PublicKey.ToHex()
            });
        }
        // Check profit receiver and profit details
        {
            var getProfitReceiver = await GetProfitReceiver(newKeyPair.PublicKey.ToHex());
            getProfitReceiver.ShouldBe(Address.FromPublicKey(newKeyPair.PublicKey));

            var oldCandidateShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(announceElectionKeyPair.PublicKey));
            oldCandidateShare.Details.Count.ShouldBe(1);
            oldCandidateShare.Details.First().Shares.ShouldBe(1);
            oldCandidateShare.Details.First().IsWeightRemoved.ShouldBeTrue();

            var newCandidateShare = await GetBackupSubsidyProfitDetails(Address.FromPublicKey(newKeyPair.PublicKey));
            newCandidateShare.Details.Count.ShouldBe(1);
            newCandidateShare.Details.First().Shares.ShouldBe(1);
            newCandidateShare.Details.First().IsWeightRemoved.ShouldBeFalse();
        }
        // Set new profit receiver 
        {
            var candidateAdminStub =
                GetTester<TreasuryContractImplContainer.TreasuryContractImplStub>(TreasuryContractAddress,
                    candidateAdmin);
            await candidateAdminStub.SetProfitsReceiver.SendAsync(
                new Treasury.SetProfitsReceiverInput
                {
                    Pubkey = newKeyPair.PublicKey.ToHex(),
                    ProfitsReceiverAddress = Address.FromPublicKey(profitReceiver.PublicKey)
                });
            var getProfitReceiver = await GetProfitReceiver(newKeyPair.PublicKey.ToHex());
            getProfitReceiver.ShouldBe(Address.FromPublicKey(profitReceiver.PublicKey));
            // Check backup subsidy
            var profitReceiverBackShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(profitReceiver.PublicKey));
            profitReceiverBackShare.Details.Count.ShouldBe(1);
            profitReceiverBackShare.Details.First().Shares.ShouldBe(1);
            profitReceiverBackShare.Details.First().IsWeightRemoved.ShouldBeFalse();

            var oldCandidateShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(announceElectionKeyPair.PublicKey));
            oldCandidateShare.Details.Count.ShouldBe(1);
            oldCandidateShare.Details.First().Shares.ShouldBe(1);
            oldCandidateShare.Details.First().IsWeightRemoved.ShouldBeTrue();

            var newCandidateShare = await GetBackupSubsidyProfitDetails(Address.FromPublicKey(newKeyPair.PublicKey));
            newCandidateShare.Details.Count.ShouldBe(1);
            newCandidateShare.Details.First().Shares.ShouldBe(1);
            newCandidateShare.Details.First().IsWeightRemoved.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task ReplaceCandidatePubkey_SetOldCandidateProfitsReceiver_Check_BackupSubsidy()
    {
        var announceElectionKeyPair = ValidationDataCenterKeyPairs.First();
        var candidateAdmin = ValidationDataCenterKeyPairs.Last();
        var newKeyPair = ValidationDataCenterKeyPairs.Skip(1).First();
        var profitReceiver = ValidationDataCenterKeyPairs.Skip(2).First();

        var candidateAdminAddress = Address.FromPublicKey(candidateAdmin.PublicKey);
        await AnnounceElectionAsync(announceElectionKeyPair, candidateAdminAddress);

        // ReplaceCandidatePubkey
        {
            var candidateAdminStub =
                GetTester<ElectionContractImplContainer.ElectionContractImplStub>(ElectionContractAddress,
                    candidateAdmin);
            await candidateAdminStub.ReplaceCandidatePubkey.SendAsync(new ReplaceCandidatePubkeyInput
            {
                OldPubkey = announceElectionKeyPair.PublicKey.ToHex(),
                NewPubkey = newKeyPair.PublicKey.ToHex()
            });
        }
        // Set old candiate profit receiver 
        {
            var candidateAdminStub =
                GetTester<TreasuryContractImplContainer.TreasuryContractImplStub>(TreasuryContractAddress,
                    candidateAdmin);
            var result = await candidateAdminStub.SetProfitsReceiver.SendWithExceptionAsync(
                new Treasury.SetProfitsReceiverInput
                {
                    Pubkey = announceElectionKeyPair.PublicKey.ToHex(),
                    ProfitsReceiverAddress = Address.FromPublicKey(profitReceiver.PublicKey)
                });
            result.TransactionResult.Error.ShouldContain("Pubkey is not a candidate.");
        }
    }

    [Fact]
    public async Task SetProfitsReceiver_ReplaceCandidatePubkey_Check_BackupSubsidy()
    {
        var announceElectionKeyPair = ValidationDataCenterKeyPairs.First();
        var candidateAdmin = ValidationDataCenterKeyPairs.Last();
        var newKeyPair = ValidationDataCenterKeyPairs.Skip(1).First();
        var profitReceiver = ValidationDataCenterKeyPairs.Skip(2).First();

        var candidateAdminAddress = Address.FromPublicKey(candidateAdmin.PublicKey);
        await AnnounceElectionAsync(announceElectionKeyPair, candidateAdminAddress);

        {
            var candidateAdminStub =
                GetTester<TreasuryContractImplContainer.TreasuryContractImplStub>(TreasuryContractAddress,
                    candidateAdmin);
            await candidateAdminStub.SetProfitsReceiver.SendAsync(
                new Treasury.SetProfitsReceiverInput
                {
                    Pubkey = announceElectionKeyPair.PublicKey.ToHex(),
                    ProfitsReceiverAddress = Address.FromPublicKey(profitReceiver.PublicKey)
                });
            var getProfitReceiver = await GetProfitReceiver(announceElectionKeyPair.PublicKey.ToHex());
            getProfitReceiver.ShouldBe(Address.FromPublicKey(profitReceiver.PublicKey));
            // Check backup subsidy
            var profitReceiverBackShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(profitReceiver.PublicKey));
            profitReceiverBackShare.Details.Count.ShouldBe(1);
            profitReceiverBackShare.Details.First().Shares.ShouldBe(1);
            profitReceiverBackShare.Details.First().IsWeightRemoved.ShouldBeFalse();

            var oldCandidateShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(announceElectionKeyPair.PublicKey));
            oldCandidateShare.Details.Count.ShouldBe(1);
            oldCandidateShare.Details.First().Shares.ShouldBe(1);
            oldCandidateShare.Details.First().IsWeightRemoved.ShouldBeTrue();
        }
        // ReplaceCandidatePubkey
        {
            var candidateAdminStub =
                GetTester<ElectionContractImplContainer.ElectionContractImplStub>(ElectionContractAddress,
                    candidateAdmin);
            await candidateAdminStub.ReplaceCandidatePubkey.SendAsync(new ReplaceCandidatePubkeyInput
            {
                OldPubkey = announceElectionKeyPair.PublicKey.ToHex(),
                NewPubkey = newKeyPair.PublicKey.ToHex()
            });
        }
        // Check profit receiver and profit details
        {
            var getProfitReceiver = await GetProfitReceiver(newKeyPair.PublicKey.ToHex());
            getProfitReceiver.ShouldBe(Address.FromPublicKey(profitReceiver.PublicKey));

            var oldCandidateShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(announceElectionKeyPair.PublicKey));
            oldCandidateShare.Details.Count.ShouldBe(1);
            oldCandidateShare.Details.First().Shares.ShouldBe(1);
            oldCandidateShare.Details.First().IsWeightRemoved.ShouldBeTrue();

            var newCandidateShare = await GetBackupSubsidyProfitDetails(Address.FromPublicKey(newKeyPair.PublicKey));
            newCandidateShare.ShouldBe(new ProfitDetails());
            
            var profitReceiverBackShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(profitReceiver.PublicKey));
            profitReceiverBackShare.Details.Count.ShouldBe(2);
            profitReceiverBackShare.Details.First().IsWeightRemoved.ShouldBeTrue();
            profitReceiverBackShare.Details.Last().IsWeightRemoved.ShouldBeFalse();
            profitReceiverBackShare.Details.First().Id.ShouldNotBe(profitReceiverBackShare.Details.Last().Id);
        }
    }

    [Fact]
    public async Task SetProfitsReceiver_QuitElection_Check_BackupSubsidy()
    {
        var announceElectionKeyPair = ValidationDataCenterKeyPairs.First();
        var candidateAdmin = ValidationDataCenterKeyPairs.Last();
        var profitReceiver = ValidationDataCenterKeyPairs.Skip(2).First();

        var candidateAdminAddress = Address.FromPublicKey(candidateAdmin.PublicKey);
        await AnnounceElectionAsync(announceElectionKeyPair, candidateAdminAddress);

        {
            var candidateAdminStub =
                GetTester<TreasuryContractImplContainer.TreasuryContractImplStub>(TreasuryContractAddress,
                    candidateAdmin);
            await candidateAdminStub.SetProfitsReceiver.SendAsync(
                new Treasury.SetProfitsReceiverInput
                {
                    Pubkey = announceElectionKeyPair.PublicKey.ToHex(),
                    ProfitsReceiverAddress = Address.FromPublicKey(profitReceiver.PublicKey)
                });
            var getProfitReceiver = await GetProfitReceiver(announceElectionKeyPair.PublicKey.ToHex());
            getProfitReceiver.ShouldBe(Address.FromPublicKey(profitReceiver.PublicKey));
        }

        await QuitElectionAsync(announceElectionKeyPair, candidateAdmin);

        // Check profit details
        {
            var candidateShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(announceElectionKeyPair.PublicKey));
            candidateShare.Details.Count.ShouldBe(1);
            candidateShare.Details.First().Shares.ShouldBe(1);
            candidateShare.Details.First().IsWeightRemoved.ShouldBeTrue();

            var profitReceiverBackShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(profitReceiver.PublicKey));
            profitReceiverBackShare.Details.Count.ShouldBe(1);
            profitReceiverBackShare.Details.First().Shares.ShouldBe(1);
            profitReceiverBackShare.Details.First().IsWeightRemoved.ShouldBeTrue();
        }
        
        // Announce Election again
        await AnnounceElectionAsync(announceElectionKeyPair, candidateAdminAddress);
        
        // Check profit receiver and profit details
        {
            var getProfitReceiver = await GetProfitReceiver(announceElectionKeyPair.PublicKey.ToHex());
            getProfitReceiver.ShouldBe(Address.FromPublicKey(profitReceiver.PublicKey));
            
            var candidateShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(announceElectionKeyPair.PublicKey));
            candidateShare.Details.Count.ShouldBe(1);
            candidateShare.Details.First().Shares.ShouldBe(1);
            candidateShare.Details.First().IsWeightRemoved.ShouldBeTrue();

            var profitReceiverBackShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(profitReceiver.PublicKey));
            profitReceiverBackShare.Details.Count.ShouldBe(2);
            profitReceiverBackShare.Details.First().IsWeightRemoved.ShouldBeTrue();
            profitReceiverBackShare.Details.Last().IsWeightRemoved.ShouldBeFalse();
            profitReceiverBackShare.Details.First().Id.ShouldBe(profitReceiverBackShare.Details.Last().Id);
        }

    }

    [Fact]
    public async Task ReplaceCandidatePubkey_QuitElection_CheckBackupSubsidy()
    {
        var announceElectionKeyPair = ValidationDataCenterKeyPairs.First();
        var candidateAdmin = ValidationDataCenterKeyPairs.Last();
        var newKeyPair = ValidationDataCenterKeyPairs.Skip(1).First();

        var candidateAdminAddress = Address.FromPublicKey(candidateAdmin.PublicKey);
        await AnnounceElectionAsync(announceElectionKeyPair, candidateAdminAddress);

        // ReplaceCandidatePubkey
        {
            var candidateAdminStub =
                GetTester<ElectionContractImplContainer.ElectionContractImplStub>(ElectionContractAddress,
                    candidateAdmin);
            await candidateAdminStub.ReplaceCandidatePubkey.SendAsync(new ReplaceCandidatePubkeyInput
            {
                OldPubkey = announceElectionKeyPair.PublicKey.ToHex(),
                NewPubkey = newKeyPair.PublicKey.ToHex()
            });
        }

        await QuitElectionAsync(newKeyPair, candidateAdmin);

        // Check profit details
        {
            var oldCandidateShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(announceElectionKeyPair.PublicKey));
            oldCandidateShare.Details.Count.ShouldBe(1);
            oldCandidateShare.Details.First().Shares.ShouldBe(1);
            oldCandidateShare.Details.First().IsWeightRemoved.ShouldBeTrue();

            var newCandidateShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(newKeyPair.PublicKey));
            newCandidateShare.Details.Count.ShouldBe(1);
            newCandidateShare.Details.First().Shares.ShouldBe(1);
            newCandidateShare.Details.First().IsWeightRemoved.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task SetProfitsReceiver_with_otherCandidateReceiver()
    {
        var announceElectionKeyPairA = ValidationDataCenterKeyPairs.First();
        var announceElectionKeyPairB = ValidationDataCenterKeyPairs.Skip(1).First();
        var profitReceiverA = ValidationDataCenterKeyPairs.Skip(2).First();
        var profitReceiverB = ValidationDataCenterKeyPairs.Skip(3).First();

        await AnnounceElectionAsync(announceElectionKeyPairA);
        await AnnounceElectionAsync(announceElectionKeyPairB);

        {
            var stub =
                GetTester<TreasuryContractImplContainer.TreasuryContractImplStub>(TreasuryContractAddress,
                    announceElectionKeyPairA);
            await stub.SetProfitsReceiver.SendAsync(new Treasury.SetProfitsReceiverInput
            {
                Pubkey = announceElectionKeyPairA.PublicKey.ToHex(),
                ProfitsReceiverAddress = Address.FromPublicKey(profitReceiverA.PublicKey)
            });
        }
        {
            var stub =
                GetTester<TreasuryContractImplContainer.TreasuryContractImplStub>(TreasuryContractAddress,
                    announceElectionKeyPairB);
            await stub.SetProfitsReceiver.SendAsync(new Treasury.SetProfitsReceiverInput
            {
                Pubkey = announceElectionKeyPairB.PublicKey.ToHex(),
                ProfitsReceiverAddress = Address.FromPublicKey(profitReceiverA.PublicKey)
            });
        }
        // Check profit details 
        {
            var candidateShareA =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(announceElectionKeyPairA.PublicKey));
            candidateShareA.Details.Count.ShouldBe(1);
            candidateShareA.Details.First().Shares.ShouldBe(1);
            candidateShareA.Details.First().IsWeightRemoved.ShouldBeTrue();

            var candidateShareB =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(announceElectionKeyPairB.PublicKey));
            candidateShareB.Details.Count.ShouldBe(1);
            candidateShareB.Details.First().Shares.ShouldBe(1);
            candidateShareB.Details.First().IsWeightRemoved.ShouldBeTrue();

            var profitReceiverBackShareA =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(profitReceiverA.PublicKey));
            profitReceiverBackShareA.Details.Count.ShouldBe(2);
            profitReceiverBackShareA.Details.All(p => p.IsWeightRemoved.Equals(false)).ShouldBeTrue();
        }

        {
            var stub =
                GetTester<TreasuryContractImplContainer.TreasuryContractImplStub>(TreasuryContractAddress,
                    announceElectionKeyPairB);
            await stub.SetProfitsReceiver.SendAsync(new Treasury.SetProfitsReceiverInput
            {
                Pubkey = announceElectionKeyPairB.PublicKey.ToHex(),
                ProfitsReceiverAddress = Address.FromPublicKey(profitReceiverB.PublicKey)
            });
        }

        // Check profit details 
        {
            var profitReceiverBackShareA =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(profitReceiverA.PublicKey));
            profitReceiverBackShareA.Details.Count.ShouldBe(2);
            profitReceiverBackShareA.Details.Where(p => p.IsWeightRemoved.Equals(false)).ToList().Count.ShouldBe(1);
            profitReceiverBackShareA.Details.Where(p => p.IsWeightRemoved.Equals(true)).ToList().Count.ShouldBe(1);

            var profitReceiverBackShareB =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(profitReceiverB.PublicKey));
            profitReceiverBackShareB.Details.Count.ShouldBe(1);
            profitReceiverBackShareB.Details.First().IsWeightRemoved.ShouldBeFalse();
        }
    }

    [Fact]
    public async Task DataCenterListContainsNode_AddBeneficiary_RemoveBeneficiary()
    {
        var minersCount = await ElectionContractStub.GetMinersCount.CallAsync(new Empty());
        var validationDataCenterCount = minersCount.Value.Mul(5);
        
        var announceElectionKeyPairA = ValidationDataCenterKeyPairs.Skip(1).First();
        var withoutDataCenterKeyPair = ValidationDataCenterKeyPairs.Skip(validationDataCenterCount + 2).First();
        var profitReceiverA = ValidationDataCenterKeyPairs.Skip(validationDataCenterCount + 3).First();
        var profitReceiverB = ValidationDataCenterKeyPairs.Skip(validationDataCenterCount + 4).First();

        for (var i = 0; i < validationDataCenterCount; i++)
        {
            var announceElectionKeyPair = ValidationDataCenterKeyPairs.Skip(i + 1).First();
            await AnnounceElectionAsync(announceElectionKeyPair);
        }

        await AnnounceElectionAsync(withoutDataCenterKeyPair);
        
        // Check profit details 
        {
            var candidateShareWithDataCenter =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(announceElectionKeyPairA.PublicKey));
            candidateShareWithDataCenter.Details.Count.ShouldBe(1);
            candidateShareWithDataCenter.Details.First().Shares.ShouldBe(1);
            candidateShareWithDataCenter.Details.First().IsWeightRemoved.ShouldBeFalse();
            
            var candidateShareWithoutDataCenter =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(withoutDataCenterKeyPair.PublicKey));
            candidateShareWithoutDataCenter.ShouldBe(new ProfitDetails());
        }

        // set  profit receiver
        {
            var candidateAdminStub =
                GetTester<TreasuryContractImplContainer.TreasuryContractImplStub>(TreasuryContractAddress,
                    announceElectionKeyPairA);
            await candidateAdminStub.SetProfitsReceiver.SendAsync(
                new Treasury.SetProfitsReceiverInput
                {
                    Pubkey = announceElectionKeyPairA.PublicKey.ToHex(),
                    ProfitsReceiverAddress = Address.FromPublicKey(profitReceiverA.PublicKey)
                });
            var getProfitReceiverA = await GetProfitReceiver(announceElectionKeyPairA.PublicKey.ToHex());
            getProfitReceiverA.ShouldBe(Address.FromPublicKey(profitReceiverA.PublicKey));
            
            candidateAdminStub =
                GetTester<TreasuryContractImplContainer.TreasuryContractImplStub>(TreasuryContractAddress,
                    withoutDataCenterKeyPair);
            await candidateAdminStub.SetProfitsReceiver.SendAsync(
                new Treasury.SetProfitsReceiverInput
                {
                    Pubkey = withoutDataCenterKeyPair.PublicKey.ToHex(),
                    ProfitsReceiverAddress = Address.FromPublicKey(profitReceiverB.PublicKey)
                });
            var getProfitReceiverB = await GetProfitReceiver(withoutDataCenterKeyPair.PublicKey.ToHex());
            getProfitReceiverB.ShouldBe(Address.FromPublicKey(profitReceiverB.PublicKey));
        }

        // Check profit details 
        {
            var oldCandidateShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(announceElectionKeyPairA.PublicKey));
            oldCandidateShare.Details.Count.ShouldBe(1);
            oldCandidateShare.Details.First().Shares.ShouldBe(1);
            oldCandidateShare.Details.First().IsWeightRemoved.ShouldBeTrue();
            
            var profitReceiverBackShareA =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(profitReceiverA.PublicKey));
            profitReceiverBackShareA.Details.Count.ShouldBe(1);
            profitReceiverBackShareA.Details.First().Shares.ShouldBe(1);
            profitReceiverBackShareA.Details.First().IsWeightRemoved.ShouldBeFalse();
            
            var profitReceiverBackShareB =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(profitReceiverB.PublicKey));
            profitReceiverBackShareB.ShouldBe(new ProfitDetails());

        }
        
        // Change DataCenter through QuitElection and check profit details
        {
            await QuitElectionAsync(announceElectionKeyPairA);
            
            var profitReceiverBackShareA =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(profitReceiverA.PublicKey));
            profitReceiverBackShareA.Details.Count.ShouldBe(1);
            profitReceiverBackShareA.Details.First().Shares.ShouldBe(1);
            profitReceiverBackShareA.Details.First().IsWeightRemoved.ShouldBeTrue();
            
            var profitReceiverBackShareB =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(profitReceiverB.PublicKey));
            profitReceiverBackShareB.Details.Count.ShouldBe(1);
            profitReceiverBackShareB.Details.First().Shares.ShouldBe(1);
            profitReceiverBackShareB.Details.First().IsWeightRemoved.ShouldBeFalse();
        }
    }

    [Fact]
    public async Task SetProfitsReceiver_Wrong_Admin_Test()
    {
        var announceElectionKeyPair = ValidationDataCenterKeyPairs.First();
        var candidateAdmin = ValidationDataCenterKeyPairs.Last();
        var profitReceiver = candidateAdmin;
        var candidateAdminAddress = Address.FromPublicKey(candidateAdmin.PublicKey);
        await AnnounceElectionAsync(announceElectionKeyPair, candidateAdminAddress);

        var candidateAdminStub =
            GetTester<TreasuryContractImplContainer.TreasuryContractImplStub>(TreasuryContractAddress,
                candidateAdmin);
        await candidateAdminStub.SetProfitsReceiver.SendAsync(
            new Treasury.SetProfitsReceiverInput
            {
                Pubkey = announceElectionKeyPair.PublicKey.ToHex(),
                ProfitsReceiverAddress = Address.FromPublicKey(profitReceiver.PublicKey)
            });
        var getProfitReceiver = await GetProfitReceiver(announceElectionKeyPair.PublicKey.ToHex());
        getProfitReceiver.ShouldBe(Address.FromPublicKey(profitReceiver.PublicKey));

        var wrongAdminStub =
            GetTester<TreasuryContractImplContainer.TreasuryContractImplStub>(TreasuryContractAddress,
                announceElectionKeyPair);
        var setResult = await wrongAdminStub.SetProfitsReceiver.SendWithExceptionAsync(
            new Treasury.SetProfitsReceiverInput(new Treasury.SetProfitsReceiverInput
            {
                Pubkey = announceElectionKeyPair.PublicKey.ToHex(),
                ProfitsReceiverAddress = Address.FromPublicKey(profitReceiver.PublicKey)
            }));
        
        setResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        setResult.TransactionResult.Error.ShouldContain("No permission.");
    }

    private async Task<ProfitDetails> GetBackupSubsidyProfitDetails(Address address)
    {
        return await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
        {
            SchemeId = ProfitItemsIds[ProfitType.BackupSubsidy],
            Beneficiary = address
        });
    }

    private async Task<Address> GetProfitReceiver(string publicKey)
    {
        return await TreasuryContractStub.GetProfitsReceiver.CallAsync(new StringValue
        {
            Value = publicKey
        });
    }
}