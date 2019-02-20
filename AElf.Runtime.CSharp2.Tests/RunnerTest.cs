using System.Linq;
using Xunit;
using AElf.Common;
using AElf.Kernel.Types.SmartContract;
using AElf.Types.CSharp;

namespace AElf.Runtime.CSharp2.Tests
{
    /*
    public class RunnerTest : CSharpRuntimeTestBase
    {
        private MockSetup _mock;
        private readonly TestContractShim _contract1;

        public RunnerTest()
        {
            _mock = GetRequiredService<MockSetup>();
            _contract1 = new TestContractShim(_mock);
        }

        [Fact(Skip = "Out of date.")]
        public void Test()
        {
            Address account0 = Address.Generate();

            _contract1.Initialize("ELF", "ELF Token", 1000000000, 0);
            Assert.Equal(1000000000UL, _contract1.BalanceOf(Address.Zero));
            _contract1.Transfer(Address.Zero, account0, 90);
            Assert.Equal(1000000000UL - 90UL, _contract1.BalanceOf(Address.Zero));
            Assert.Equal(90UL, _contract1.BalanceOf(account0));
            _contract1.SetMethodFee("Transfer", 100UL);
            Assert.Equal(100UL, _contract1.GetMethodFee("Transfer"));

            // Test transaction fee
            _contract1.TransferAndGetTrace(Address.Zero, account0, 100, out var trace);
            var feeTxn =
                trace.InlineTransactions.Single(tr => tr.MethodName == nameof(ITokenCotract.ChargeTransactionFees));
            var feeAmount = (ulong) ParamsPacker.Unpack(feeTxn.Params.ToByteArray(), new[] {typeof(ulong)}).First();
            Assert.Equal(100UL, feeAmount);
        }
    }
    */
}