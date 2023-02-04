using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Profit;
using AElf.Contracts.Treasury;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election;

public partial class ElectionContractTests
{
    [Fact]
    public async Task Announcement_Check_BackupSubsidy()
    {
        var announceElectionKeyPair = ValidationDataCenterKeyPairs.First();
        var candidateAdmin = ValidationDataCenterKeyPairs.Last();
        var candidateAdminAddress = Address.FromPublicKey(candidateAdmin.PublicKey);
        await AnnounceElectionAsync(announceElectionKeyPair, candidateAdminAddress);

        var candidateBackupShare = await GetBackupSubsidyProfitDetails(Address.FromPublicKey(announceElectionKeyPair.PublicKey));
        candidateBackupShare.Details.Count.ShouldBe(1);
        candidateBackupShare.Details.First().Shares.ShouldBe(1);
    }

    [Fact]
    public async Task Announcement_SetProfitReceiver_Check_BackupSubsidy()
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
    public async Task Announcement_SetProfitReceiver_Same_Receiver_Check_BackupSubsidy()
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
    public async Task ReplaceCandidatePubkey_SetProfitReceiver_Check_BackupSubsidy()
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
            var getProfitReceiver = await GetProfitReceiver(announceElectionKeyPair.PublicKey.ToHex());
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
            profitReceiverBackShare.Details.First().IsWeightRemoved.ShouldBeTrue();

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
    }
    
    [Fact]
    public async Task SetProfitReceiver_ReplaceCandidatePubkey_Check_BackupSubsidy()
    {
        var announceElectionKeyPair = ValidationDataCenterKeyPairs.First();
        var candidateAdmin = ValidationDataCenterKeyPairs.Last();
        var newKeyPair = ValidationDataCenterKeyPairs.Skip(1).First();
        var profitReceiver = ValidationDataCenterKeyPairs.Skip(2).First();
        var newProfitReceiver = ValidationDataCenterKeyPairs.Skip(3).First();

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
            var getProfitReceiver = await GetProfitReceiver(announceElectionKeyPair.PublicKey.ToHex());
            getProfitReceiver.ShouldBe(Address.FromPublicKey(profitReceiver.PublicKey));

            var oldCandidateShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(announceElectionKeyPair.PublicKey));
            oldCandidateShare.Details.Count.ShouldBe(1);
            oldCandidateShare.Details.First().Shares.ShouldBe(1);
            oldCandidateShare.Details.First().IsWeightRemoved.ShouldBeTrue();

            var newCandidateShare = await GetBackupSubsidyProfitDetails(Address.FromPublicKey(newKeyPair.PublicKey));
            newCandidateShare.Details.Count.ShouldBe(1);
            newCandidateShare.Details.First().Shares.ShouldBe(1);
            newCandidateShare.Details.First().IsWeightRemoved.ShouldBeTrue();
            
            var profitReceiverBackShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(profitReceiver.PublicKey));
            // profitReceiverBackShare.Details.Count.ShouldBe(1);
            profitReceiverBackShare.Details.First().Shares.ShouldBe(1);
            profitReceiverBackShare.Details.First().IsWeightRemoved.ShouldBeFalse();
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
                    ProfitsReceiverAddress = Address.FromPublicKey(newProfitReceiver.PublicKey)
                });
            var getProfitReceiver = await GetProfitReceiver(newKeyPair.PublicKey.ToHex());
            getProfitReceiver.ShouldBe(Address.FromPublicKey(newProfitReceiver.PublicKey));
            // Check backup subsidy
            var profitReceiverBackShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(profitReceiver.PublicKey));
            profitReceiverBackShare.Details.Count.ShouldBe(1);
            profitReceiverBackShare.Details.First().Shares.ShouldBe(1);
            profitReceiverBackShare.Details.First().IsWeightRemoved.ShouldBeTrue();
            
            var newProfitReceiverBackShare =
                await GetBackupSubsidyProfitDetails(Address.FromPublicKey(newProfitReceiver.PublicKey));
            newProfitReceiverBackShare.Details.Count.ShouldBe(1);
            newProfitReceiverBackShare.Details.First().Shares.ShouldBe(1);
            newProfitReceiverBackShare.Details.First().IsWeightRemoved.ShouldBeFalse();
        }
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