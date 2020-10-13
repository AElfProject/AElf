using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.ContractTestKit.AEDPoSExtension;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Token;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Economic.AEDPoSExtension.Tests
{
    public class ChargeMethodFeeTest : EconomicTestBase
    {
        [Fact]
        public async Task FreeSizeFeeTest()
        {
            var userAddress1 = Address.FromPublicKey(MissionedECKeyPairs.InitialKeyPairs.First().PublicKey);
            var userAddress2 = Address.FromPublicKey(MissionedECKeyPairs.InitialKeyPairs.Skip(1).First().PublicKey);
            var tokenStub = GetTester<TokenContractContainer.TokenContractStub>(
                ContractAddresses[TokenSmartContractAddressNameProvider.Name],
                MissionedECKeyPairs.InitialKeyPairs.First());
            
            {
                var balanceBefore = await tokenStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = userAddress1,
                    Symbol = EconomicTestConstants.TokenSymbol
                });
                await BlockMiningService.MineBlockAsync(new List<Transaction>
                {
                    tokenStub.Transfer.GetTransaction(new TransferInput
                    {
                        Symbol = EconomicTestConstants.TokenSymbol,
                        Amount = 100,
                        To = userAddress2
                    })
                });
                await BlockMiningService.MineBlockAsync();
                var balanceAfter = await TokenStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = userAddress1,
                    Symbol = EconomicTestConstants.TokenSymbol
                });
                balanceBefore.Balance.ShouldBe(balanceAfter.Balance);
            }

            UpdateParliamentStubs(MissionedECKeyPairs.InitialKeyPairs);
            var defaultOrganizationAddress =
                await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            await ParliamentReachAnAgreementAsync(new CreateProposalInput
            {
                ToAddress = ContractAddresses[ConsensusSmartContractAddressNameProvider.Name],
                ContractMethodName = nameof(TokenStub.Transfer),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = defaultOrganizationAddress,
                Params = new MethodFees
                {
                    MethodName = nameof(TokenStub.Transfer),
                    IsSizeFeeFree = true
                }.ToByteString()
            });

            {
                var balanceBefore = await tokenStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = userAddress1,
                    Symbol = EconomicTestConstants.TokenSymbol
                });
                await BlockMiningService.MineBlockAsync(new List<Transaction>
                {
                    tokenStub.Transfer.GetTransaction(new TransferInput
                    {
                        Symbol = EconomicTestConstants.TokenSymbol,
                        Amount = 100,
                        To = userAddress2
                    })
                });
                await BlockMiningService.MineBlockAsync();
                var balanceAfter = await tokenStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = userAddress1,
                    Symbol = EconomicTestConstants.TokenSymbol
                });
                balanceBefore.Balance.ShouldBe(balanceAfter.Balance);
            }
        }
    }
}