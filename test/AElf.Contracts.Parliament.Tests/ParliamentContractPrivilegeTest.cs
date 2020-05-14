using System.Threading.Tasks;
using AElf.Standards.ACS3;
using AElf.Cryptography;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Parliament
{
    public class ParliamentContractPrivilegeTest : ParliamentContractPrivilegeTestBase
    {
        [Fact]
        public async Task CreateProposal_WithPrivileged_Test()
        {
            var organizationAddress = await GetDefaultOrganizationAddressAsync();
            var ecKeyPair = CryptoHelper.GenerateKeyPair();
            var otherTester = Tester.CreateNewContractTester(ecKeyPair);
            var transferInput = TransferInput(otherTester.GetCallOwnerAddress());
            var createProposalInput = CreateProposalInput(transferInput, organizationAddress);

            var result = await otherTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractImplContainer.ParliamentContractImplStub.CreateProposal),
                createProposalInput);
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Unauthorized to propose.").ShouldBeTrue();
            
            //verify with view method
            var byteString = await otherTester.CallContractMethodAsync(ParliamentAddress,
                nameof(ParliamentContractImplContainer.ParliamentContractImplStub.ValidateProposerInWhiteList),
                new ValidateProposerInWhiteListInput
                {
                    OrganizationAddress = organizationAddress,
                    Proposer = Address.FromPublicKey(ecKeyPair.PublicKey)
                });
            var verifyResult = BoolValue.Parser.ParseFrom(byteString);
            verifyResult.Value.ShouldBeFalse();
        }

        [Fact]
        public async Task CreateProposal_Creator_Test()
        {
            var organizationAddress = await GetDefaultOrganizationAddressAsync();
            var ecKeyPair = CryptoHelper.GenerateKeyPair();
            var otherTester = Tester.CreateNewContractTester(ecKeyPair);
            var transferInput = TransferInput(otherTester.GetCallOwnerAddress());
            var createProposalInput = CreateProposalInput(transferInput, organizationAddress);

            var result = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractImplContainer.ParliamentContractImplStub.CreateProposal),
                createProposalInput);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task CreateProposal_Miner_Test()
        {
            var organizationAddress = await GetDefaultOrganizationAddressAsync();
            var miner = Tester.CreateNewContractTester(Tester.InitialMinerList[0]);
            var transferInput = TransferInput(miner.GetCallOwnerAddress());
            var createProposalInput = CreateProposalInput(transferInput, organizationAddress);

            var result = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractImplContainer.ParliamentContractImplStub.CreateProposal),
                createProposalInput);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task CreateProposal_WithoutPrivilege_Test()
        {
            var organizationAddress = await CreateOrganizationAsync(true);
            var ecKeyPair = CryptoHelper.GenerateKeyPair();
            var otherTester = Tester.CreateNewContractTester(ecKeyPair);
            var transferInput = TransferInput(otherTester.GetCallOwnerAddress());
            var createProposalInput = CreateProposalInput(transferInput, organizationAddress);

            var result = await otherTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractImplContainer.ParliamentContractImplStub.CreateProposal),
                createProposalInput);
            result.Status.ShouldBe(TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task Change_OrganizationProposalWhiteList_Test()
        {
            var organizationAddress = await GetDefaultOrganizationAddressAsync();
            var result = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractImplContainer.ParliamentContractImplStub.GetProposerWhiteList), new Empty());
            var proposers = ProposerWhiteList.Parser.ParseFrom(result.ReturnValue).Proposers;

            proposers.Count.ShouldBe(1);
            proposers.Contains(Tester.GetCallOwnerAddress()).ShouldBeTrue();
            var ecKeyPair = CryptoHelper.GenerateKeyPair();

            var proposerWhiteList = new ProposerWhiteList
            {
                Proposers = {Tester.GetAddress(ecKeyPair)}
            };
            var proposalInput = CreateParliamentProposalInput(proposerWhiteList, organizationAddress);
            var createResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractImplContainer.ParliamentContractImplStub.CreateProposal),
                proposalInput);
            createResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposalId = Hash.Parser.ParseFrom(createResult.ReturnValue);

            var miner = Tester.CreateNewContractTester(Tester.InitialMinerList[0]);
            (await miner.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentContractImplContainer.ParliamentContractImplStub.Approve), proposalId)).Status
                .ShouldBe(TransactionResultStatus.Mined);
            miner = Tester.CreateNewContractTester(Tester.InitialMinerList[1]);
            (await miner.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentContractImplContainer.ParliamentContractImplStub.Approve), proposalId)).Status
                .ShouldBe(TransactionResultStatus.Mined);
            miner = Tester.CreateNewContractTester(Tester.InitialMinerList[2]);
            (await miner.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentContractImplContainer.ParliamentContractImplStub.Approve), proposalId)).Status
                .ShouldBe(TransactionResultStatus.Mined);

            var releaseResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractImplContainer.ParliamentContractImplStub.Release), proposalId);
            releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);

            result = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractImplContainer.ParliamentContractImplStub.GetProposerWhiteList), new Empty());
            proposers = ProposerWhiteList.Parser.ParseFrom(result.ReturnValue).Proposers;
            proposers.Count.ShouldBe(1);
            proposers.Contains(Tester.GetAddress(ecKeyPair)).ShouldBeTrue();
            proposers.Contains(Tester.GetCallOwnerAddress()).ShouldBeFalse();
        }

        [Fact]
        public async Task ValidateAddressIsParliamentMember_Test()
        {
            //miner member
            var byteResult = await Tester.CallContractMethodAsync(
                ParliamentAddress,
                nameof(ParliamentContractImplContainer.ParliamentContractImplStub.ValidateAddressIsParliamentMember),
                Address.FromPublicKey(Tester.InitialMinerList[0].PublicKey));
            var checkMinerResult = BoolValue.Parser.ParseFrom(byteResult);
            checkMinerResult.Value.ShouldBeTrue();
            
            //none miner member
            var tester = Address.FromPublicKey(CryptoHelper.GenerateKeyPair().PublicKey);
            byteResult = await Tester.CallContractMethodAsync(
                ParliamentAddress,
                nameof(ParliamentContractImplContainer.ParliamentContractImplStub.ValidateAddressIsParliamentMember),
                tester);
            var checkTesterResult = BoolValue.Parser.ParseFrom(byteResult);
            checkTesterResult.Value.ShouldBeFalse();
        }

        private async Task<Address> CreateOrganizationAsync(bool proposerAuthorityRequired = false)
        {
            var minimalApprovalThreshold = 6667;
            var maximalAbstentionThreshold = 2000;
            var maximalRejectionThreshold = 3000;
            var minimalVoteThreshold = 8000;
            var createOrganizationInput = new CreateOrganizationInput
            {
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MinimalApprovalThreshold = minimalApprovalThreshold,
                    MaximalAbstentionThreshold = maximalAbstentionThreshold,
                    MaximalRejectionThreshold = maximalRejectionThreshold,
                    MinimalVoteThreshold = minimalVoteThreshold
                },
                ProposerAuthorityRequired = proposerAuthorityRequired
            };
            var transactionResult =
                await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentContractImplContainer.ParliamentContractImplStub.CreateOrganization),
                    createOrganizationInput);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            return Address.Parser.ParseFrom(transactionResult.ReturnValue);
        }

        private async Task<Address> GetDefaultOrganizationAddressAsync()
        {
            var result = (await Tester.ExecuteContractWithMiningAsync(
                ParliamentAddress,
                nameof(ParliamentContractImplContainer.ParliamentContractImplStub.GetDefaultOrganizationAddress),
                new Empty()));
            return Address.Parser.ParseFrom(result.ReturnValue);
        }
    }
}