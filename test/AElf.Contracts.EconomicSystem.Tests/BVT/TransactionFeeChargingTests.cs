using System.Linq;
using System.Threading.Tasks;
using Acs1;
using AElf.Contracts.MultiToken;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.EconomicSystem.Tests.BVT
{
    public class TransactionFeeChargingTests : EconomicSystemTestBase
    {
        public TransactionFeeChargingTests()
        {
            InitializeContracts();
        }
        
        [Fact]
        public async Task TransactionFee_Mined_Test()
        {
            //set method fee
            var setResult = await TransactionFeeChargingContractStub.SetMethodFee.SendAsync(new MethodFees
            {
                MethodName = "SendForFun",
                Fees =
                {
                    new MethodFee {Symbol = "ELF", BasicFee = 1_0000_0000}
                }
            });
            setResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var tester = InitialCoreDataCenterKeyPairs.First();
            var beforeBalance = await GetUserBalance(tester);

            var transactionStub = GetTransactionFeeChargingContractTester(tester);
            var transactionResult = await transactionStub.SendForFun.SendAsync(new Empty());
            var transactionSize = transactionResult.Transaction.Size();
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var afterBalance = await GetUserBalance(tester);
            beforeBalance.ShouldBe(afterBalance + 1_0000_0000 + transactionSize * 0);
        }

        [Fact(Skip = "Current failed transaction cannot charge fee")]
        public async Task TransactionFee_Failed_Test()
        {
            var setResult = await TransactionFeeChargingContractStub.SetMethodFee.SendAsync(new MethodFees
            {
                MethodName = "SupposedToFail",
                Fees =
                {
                    new MethodFee {Symbol = "ELF", BasicFee = 2_0000_0000}
                }
            });
            setResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var tester = InitialCoreDataCenterKeyPairs.First();
            var beforeBalance = await GetUserBalance(tester);
            
            var transactionStub = GetTransactionFeeChargingContractTester(tester);
            var transactionResult = await transactionStub.SupposedToFail.SendAsync(new Empty());
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);

            var afterBalance = await GetUserBalance(tester);
            beforeBalance.ShouldBe(afterBalance + 2_0000_0000);
        }

        private async Task<long> GetUserBalance(ECKeyPair keyPair)
        {
            var balanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Address.FromPublicKey(keyPair.PublicKey),
                Symbol = "ELF"
            });

            return balanceOutput.Balance;
        }
    }
}