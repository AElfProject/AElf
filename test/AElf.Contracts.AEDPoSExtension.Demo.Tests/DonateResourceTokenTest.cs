using System.Linq;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.AEDPoSExtension.Demo.Tests
{
    public class DonateResourceTokenTest : AEDPoSExtensionDemoTestBase
    {
        private const int CpuAmount = 4;
        private const int RamAmount = 8;
        private const int DiskAmount = 512;

        private const long Rental = 100;

        private const long ResourceSupply = 1_0000_0000_00000000;

        private static Address Creator => Address.FromPublicKey(SampleECKeyPairs.KeyPairs[0].PublicKey);

        [Fact]
        public async Task ChargeRentalTest()
        {
            await InitialTokenContract();

            // Check balance before mining
            {
                var cpuBalance = await GetCreatorBalanceOf("CPU");
                cpuBalance.ShouldBe(ResourceSupply);
                var ramBalance = await GetCreatorBalanceOf("RAM");
                ramBalance.ShouldBe(ResourceSupply);
                var diskBalance = await GetCreatorBalanceOf("DISK");
                diskBalance.ShouldBe(ResourceSupply);
            }

            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();

            // Check balance before mining
            {
                var cpuBalance = await GetCreatorBalanceOf("CPU");
                cpuBalance.ShouldBe(ResourceSupply - CpuAmount * Rental);
                var ramBalance = await GetCreatorBalanceOf("RAM");
                ramBalance.ShouldBe(ResourceSupply - RamAmount * Rental);
                var diskBalance = await GetCreatorBalanceOf("DISK");
                diskBalance.ShouldBe(ResourceSupply - DiskAmount * Rental);
            }
        }

        private async Task InitialTokenContract()
        {
            if (!ParliamentStubs.Any())
            {
                InitialAcs3Stubs();
            }

            await ParliamentStubs.First().Initialize.SendAsync(new ParliamentAuth.InitializeInput
            {
                GenesisOwnerReleaseThreshold = 6666
            });
            var defaultOrganizationAddress =
                await ParliamentStubs.First().GetDefaultOrganizationAddress.CallAsync(new Empty());

            await ParliamentReachAnAgreementAsync(new CreateProposalInput
            {
                ToAddress = ContractAddresses[TokenSmartContractAddressNameProvider.Name],
                ContractMethodName = nameof(TokenContractContainer.TokenContractStub.Initialize),
                Params = new InitializeInput
                {
                    ResourceAmount =
                    {
                        {"CPU", CpuAmount},
                        {"RAM", RamAmount},
                        {"DISK", DiskAmount}
                    }
                }.ToByteString(),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = defaultOrganizationAddress
            });

            await ParliamentReachAnAgreementAsync(new CreateProposalInput
            {
                ToAddress = ContractAddresses[TokenSmartContractAddressNameProvider.Name],
                ContractMethodName = nameof(TokenContractContainer.TokenContractStub.SetSideChainCreator),
                Params = Creator.ToByteString(),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = defaultOrganizationAddress
            });

            await TokenStub.UpdateRental.SendAsync(new UpdateRentalInput
            {
                Rental =
                {
                    {"CPU", Rental},
                    {"RAM", Rental},
                    {"DISK", Rental},
                }
            });

            await CreateToken("CPU", ResourceSupply);
            await CreateToken("RAM", ResourceSupply);
            await CreateToken("DISK", ResourceSupply);
        }

        private async Task CreateToken(string symbol, long supply)
        {
            await TokenStub.Create.SendAsync(new CreateInput
            {
                Decimals = 8,
                Issuer = Creator,
                Symbol = symbol,
                TotalSupply = supply,
                IsBurnable = true,
                TokenName = $"{symbol} token."
            });

            await TokenStub.Issue.SendAsync(new IssueInput
            {
                Symbol = symbol,
                To = Creator,
                Amount = supply
            });
        }

        private async Task<long> GetCreatorBalanceOf(string symbol)
        {
            return (await TokenStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Creator,
                Symbol = symbol
            })).Balance;
        }
    }
}