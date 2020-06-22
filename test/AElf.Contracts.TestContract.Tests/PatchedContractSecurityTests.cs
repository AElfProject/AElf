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
        public async Task ResetFields_Test()
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
            
            var allFieldReset = await TestBasicSecurityContractStub.CheckFieldsAlreadyReset.CallAsync(new Empty());
            allFieldReset.Value.ShouldBeTrue();
        }
        
        // [Fact]
        // public async Task Reset_NestedFields_Test()
        // {
        //     var result = await TestBasicSecurityContractStub.TestResetNestedFields.SendAsync(new ResetNestedInput
        //     {
        //         Int32Value = 100,
        //         StringValue = "TEST"
        //     });
        //     result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        //     result.Output.Int32Value.ShouldBe(100);
        //     result.Output.StringValue.ShouldBe("TEST");
        //     var fields = await TestBasicSecurityContractStub.QueryNestedFields.CallAsync(new Empty());
        //     fields.Int32Value.ShouldBe(0);
        //     fields.StringValue.ShouldBe(string.Empty);
        //     
        //     var allFieldReset = await TestBasicSecurityContractStub.CheckFieldsAlreadyReset.CallAsync(new Empty());
        //     allFieldReset.Value.ShouldBeTrue();
        // }
        //
        // [Fact]
        // public async Task Reset_OtherType_NestedFields_Test()
        // {
        //     var result = await TestBasicSecurityContractStub.TestResetOtherTypeFields.SendAsync(new ResetNestedInput
        //     {
        //         Int32Value = 100,
        //         StringValue = "TEST"
        //     });
        //     result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        //     result.Output.StringValue.ShouldBe("test");
        //     result.Output.BasicTypeNumber.ShouldBe(100);
        //     result.Output.BasicTypeStaticNumber.ShouldBe(100);
        //     result.Output.TypeConst.ShouldBe(1);
        //     result.Output.TypeNumber.ShouldBe(100);
        //     
        //     
        //     var allFieldReset = await TestBasicSecurityContractStub.CheckFieldsAlreadyReset.CallAsync(new Empty());
        //     allFieldReset.Value.ShouldBeTrue();
        //     
        //     var allStaticFieldsReset = await TestBasicSecurityContractStub.CheckNonContractTypesStaticFieldsReset.CallAsync(new Empty());
        //     allStaticFieldsReset.Value.ShouldBeTrue();
        // }
    }
}