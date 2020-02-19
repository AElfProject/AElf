using System.Threading.Tasks;
using AElf.Contracts.TestContract.BasicSecurity;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contract.TestContract
{
    public class PatchedContractSecurityTests : TestContractTestBase
    {
        public PatchedContractSecurityTests()
        {
            InitializePatchedContracts();
        }
        
        [Fact]
        public async Task ResetFields()
        {
            var result = await TestBasicSecurityContractStub.TestResetFields.SendAsync(new ResetInput
            {
                BoolValue = true,
                Int32Value = 100,
                Int64Value = 1000,
                StringValue = "TEST"
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var int64 = await TestBasicSecurityContractStub.QueryInt64State.CallAsync(new Empty());
            var s = await TestBasicSecurityContractStub.QueryStringState.CallAsync(new Empty());
            var constValue = await TestBasicSecurityContractStub.QueryConst.CallAsync(new Empty());
            int64.Int64Value.Equals(constValue.Int64Const).ShouldBeTrue();
            s.StringValue.Equals(constValue.StringConst).ShouldBeTrue();
            
            var fields = await TestBasicSecurityContractStub.QueryFields.CallAsync(new Empty());
            fields.BoolValue.ShouldBeFalse();
            fields.Int32Value.ShouldBe(0);
            fields.Int64Value.ShouldBe(0);
            fields.StringValue.ShouldBe(string.Empty);
        }
    }
}