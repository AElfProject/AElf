using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Contracts.EconomicSystem.Tests.BVT
{
    public partial class EconomicSystemTest
    {
        private const long Amount = 100;
        private const string ResourceTokenSymbol = "NET";

        [Fact]
        public async Task<Address> TokenContract_AdvanceResourceToken()
        {
            var contractAddress = SampleAddress.AddressList[0];
            var developerAddress = BootMinerAddress;

            var balanceBeforeAdvancing = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = developerAddress,
                Symbol = ResourceTokenSymbol
            });

            await TokenContractStub.AdvanceResourceToken.SendAsync(new AdvanceResourceTokenInput
            {
                ContractAddress = contractAddress,
                Amount = Amount,
                ResourceTokenSymbol = ResourceTokenSymbol
            });

            // Check balance of contract address.
            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = contractAddress,
                    Symbol = ResourceTokenSymbol
                });
                balance.Balance.ShouldBe(Amount);
            }

            // Check balance of developer.
            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = developerAddress,
                    Symbol = ResourceTokenSymbol
                });
                balance.Balance.ShouldBe(balanceBeforeAdvancing.Balance - Amount);
            }

            return contractAddress;
        }

        [Fact]
        public async Task TokenContract_TakeResourceTokenBack()
        {
            var contractAddress = await TokenContract_AdvanceResourceToken();
            var developerAddress = BootMinerAddress;

            var balanceBeforeTakingBack = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = developerAddress,
                Symbol = ResourceTokenSymbol
            });

            await TokenContractStub.TakeResourceTokenBack.SendAsync(new TakeResourceTokenBackInput
            {
                ContractAddress = contractAddress,
                Amount = Amount,
                ResourceTokenSymbol = ResourceTokenSymbol
            });

            // Check balance of developer.
            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = developerAddress,
                    Symbol = ResourceTokenSymbol
                });
                balance.Balance.ShouldBe(balanceBeforeTakingBack.Balance + Amount);
            }

            // Check balance of contract address.
            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = contractAddress,
                    Symbol = ResourceTokenSymbol
                });
                balance.Balance.ShouldBe(0);
            }
        }

        [Fact]
        public async Task TokenContract_TakeResourceTokenBack_NotAll()
        {
            var contractAddress = await TokenContract_AdvanceResourceToken();
            var developerAddress = BootMinerAddress;
            const long takeBackAmount = Amount / 2;

            var balanceBeforeTakingBack = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = developerAddress,
                Symbol = ResourceTokenSymbol
            });

            await TokenContractStub.TakeResourceTokenBack.SendAsync(new TakeResourceTokenBackInput
            {
                ContractAddress = contractAddress,
                Amount = takeBackAmount,
                ResourceTokenSymbol = ResourceTokenSymbol
            });

            // Check balance of developer.
            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = developerAddress,
                    Symbol = ResourceTokenSymbol
                });
                balance.Balance.ShouldBe(balanceBeforeTakingBack.Balance + takeBackAmount);
            }

            // Check balance of contract address.
            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = contractAddress,
                    Symbol = ResourceTokenSymbol
                });
                balance.Balance.ShouldBe(Amount - takeBackAmount);
            }
        }

        [Fact]
        public async Task TokenContract_TakeResourceTokenBack_Exceed()
        {
            var contractAddress = await TokenContract_AdvanceResourceToken();
            const long takeBackAmount = Amount * 2;

            var result = await TokenContractStub.TakeResourceTokenBack.SendAsync(new TakeResourceTokenBackInput
            {
                ContractAddress = contractAddress,
                Amount = takeBackAmount,
                ResourceTokenSymbol = ResourceTokenSymbol
            });

            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.ShouldContain("Can't take back that more.");
        }
    }
}