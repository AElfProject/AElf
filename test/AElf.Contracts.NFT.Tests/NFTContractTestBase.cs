using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Standards.ACS2;
using AElf.Contracts.Parliament;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using AElf.Contracts.Treasury;
using AElf.Contracts.TokenConverter;
using AElf.ContractTestBase.ContractTestKit;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Standards.ACS3;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;

namespace AElf.Contracts.NFT
{
    public class NFTContractTestBase : ContractTestBase<NFTContractTestAElfModule>
    {
        protected ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
        protected Address DefaultAddress => Accounts[0].Address;
        protected ECKeyPair MinterKeyPair => Accounts[1].KeyPair;
        protected Address MinterAddress => Accounts[1].Address;

        protected ECKeyPair User1KeyPair => Accounts[10].KeyPair;
        protected Address User1Address => Accounts[10].Address;
        protected Address User2Address => Accounts[11].Address;

        protected const long Amount = 100;

        protected List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
            Accounts.Take(InitialCoreDataCenterCount).Select(a => a.KeyPair).ToList();

        internal TokenContractImplContainer.TokenContractImplStub TokenContractStub;

        internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub;

        protected Hash NFTContractName => HashHelper.ComputeFrom("AElf.ContractNames.NFT");
        protected Address NFTContractAddress { get; set; }
        internal NFTContractContainer.NFTContractStub NFTContractStub { get; set; }
        internal NFTContractContainer.NFTContractStub MinterNFTContractStub { get; set; }

        public NFTContractTestBase()
        {
            TokenContractStub =
                GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, DefaultKeyPair);

            NFTContractAddress = SystemContractAddresses[NFTContractName];
            NFTContractStub = GetTester<NFTContractContainer.NFTContractStub>(NFTContractAddress, DefaultKeyPair);
            MinterNFTContractStub = GetTester<NFTContractContainer.NFTContractStub>(NFTContractAddress, MinterKeyPair);

            ParliamentContractStub = GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(
                ParliamentContractAddress, DefaultKeyPair);

            AsyncHelper.RunSync(CreateNativeTokenAsync);
            AsyncHelper.RunSync(SetNFTContractAddress);
        }

        internal ParliamentContractImplContainer.ParliamentContractImplStub GetParliamentContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress,
                keyPair);
        }

        private async Task CreateNativeTokenAsync()
        {
            await TokenContractStub.Create.SendAsync(new MultiToken.CreateInput()
            {
                Symbol = NativeTokenInfo.Symbol,
                TokenName = NativeTokenInfo.TokenName,
                TotalSupply = NativeTokenInfo.TotalSupply,
                Decimals = NativeTokenInfo.Decimals,
                Issuer = NativeTokenInfo.Issuer,
                IsBurnable = NativeTokenInfo.IsBurnable
            });
        }

        private TokenInfo NativeTokenInfo => new TokenInfo
        {
            Symbol = "ELF",
            TokenName = "Native token",
            TotalSupply = 10_00000000_00000000,
            Decimals = 8,
            IsBurnable = true,
            Issuer = DefaultAddress
        };

        private async Task SetNFTContractAddress()
        {
            var defaultParliament = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            var proposalId = await CreateProposalAsync(TokenContractAddress,
                defaultParliament, nameof(TokenContractStub.AddAddressToCreateTokenWhiteList),
                NFTContractAddress);
            await ApproveWithMinersAsync(proposalId);
            await ParliamentContractStub.Release.SendAsync(proposalId);
        }

        private async Task<Hash> CreateProposalAsync(Address contractAddress, Address organizationAddress,
            string methodName, IMessage input)
        {
            var proposal = new CreateProposalInput
            {
                OrganizationAddress = organizationAddress,
                ContractMethodName = methodName,
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
                Params = input.ToByteString(),
                ToAddress = contractAddress
            };

            var createResult = await ParliamentContractStub.CreateProposal.SendAsync(proposal);
            var proposalId = createResult.Output;

            return proposalId;
        }

        private async Task ApproveWithMinersAsync(Hash proposalId)
        {
            foreach (var bp in InitialCoreDataCenterKeyPairs)
            {
                var tester = GetParliamentContractTester(bp);
                await tester.Approve.SendAsync(proposalId);
            }
        }
    }
}