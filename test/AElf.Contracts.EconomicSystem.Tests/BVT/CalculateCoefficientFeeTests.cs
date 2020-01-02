using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Contracts.EconomicSystem.Tests.BVT
{
    public partial class EconomicSystemTest
    {
        [Fact]
        public async Task UpdateCoefficient_NotChange_Test()
        {
            //invalid setting
            var tokenContractImplStub = GetTokenContractImplTester(InitialCoreDataCenterKeyPairs.Last());
            var txResult =
                await tokenContractImplStub.UpdateCoefficientFromContract.SendAsync(new CoefficientFromContract());
            txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            txResult.TransactionResult.Error.ShouldContain("No permission ");

            txResult = await tokenContractImplStub.UpdateCoefficientFromSender.SendAsync(new CoefficientFromSender());
            txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            txResult.TransactionResult.Error.ShouldContain("No permission ");
        }

        [Fact]
        public async Task UpdateCoefficient_TxFee_Test()
        {
            var exception = await Record.ExceptionAsync(() =>
                ExecuteProposalTransaction(Tester, TokenContractAddress, "UpdateCoefficientFromContract",
                    new CoefficientFromContract
                    {
                        FeeType = FeeTypeEnum.Tx,
                        Coefficient = new CoefficientFromSender
                        {
                            IsChangePieceKey = false,
                            IsLiner = true,
                            PieceKey = 1000000,
                            LinerCoefficient = new LinerCoefficient
                            {
                                ConstantValue = 2000,
                                Denominator = 8,
                                Numerator = 1
                            }
                        }
                    })
            );
            exception.ShouldBeNull();
            await Task.CompletedTask;
        }

        [Fact]
        public async Task UpdateCoefficient_CpuFee_Test()
        {
            var exception = await Record.ExceptionAsync(() =>
                ExecuteProposalTransaction(Tester, TokenContractAddress, "UpdateCoefficientFromSender",
                    new CoefficientFromContract
                    {
                        FeeType = FeeTypeEnum.Read,
                        Coefficient = new CoefficientFromSender
                        {
                            IsChangePieceKey = false,
                            IsLiner = false,
                            PieceKey = int.MaxValue,
                            PowerCoefficient = new PowerCoefficient
                            {
                                Numerator = 2,
                                Denominator = 4,
                                ChangeSpanBase = 5,
                                Weight = 300,
                                WeightBase = 50
                            }
                        }
                    })
            );
            exception.ShouldBeNull();
        }
    }
}