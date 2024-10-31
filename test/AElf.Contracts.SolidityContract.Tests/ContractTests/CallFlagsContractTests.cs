using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Scale;
using Shouldly;
using Xunit.Abstractions;

namespace AElf.Contracts.SolidityContract;

public class CallFlagsContractTest : SolidityContractTestBase
{
    private const uint Voyager = 987654321;

    public CallFlagsContractTest(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        ContractPath = "contracts/CallFlags.contract";
    }

    // See https://github.com/paritytech/substrate/blob/5ea6d95309aaccfa399c5f72e5a14a4b7c6c4ca1/frame/contracts/src/wasm/runtime.rs#L373
    [Flags]
    enum CallFlag
    {
        ForwardInput,
        CloneInput,
        TailCall,
        AllowReentry
    }

    [Fact(DisplayName = "works with the reentry flag.")]
    public async Task<Address> TestReentryFlag()
    {
        var contractAddress = await DeployContractAsync();

        var queryResult = await QueryAsync(contractAddress, "echo",
            TupleType<AddressType, BytesType, UInt32Type, VecType<EnumType<CallFlag>>>.GetByteStringFrom(
                AddressType.From(contractAddress.ToByteArray()),
                BytesType.From([0, 0, 0, 0]),
                UInt32Type.From(Voyager),
                VecType<EnumType<CallFlag>>.From(new[]
                {
                    EnumType<CallFlag>.From(CallFlag.AllowReentry)
                }.ToArray())
            ));

        var result = UInt32Type.From(queryResult.ToByteArray()).Value;
        result.ShouldBe(Voyager);

        return contractAddress;
    }

    [Fact(DisplayName = "works with the reentry and tail call flags.", Skip = "Not supported yet.")]
    public async Task<Address> TestReentryAndTailCallFlags()
    {
        var contractAddress = await DeployContractAsync();

        var queryResult = await QueryWithExceptionAsync(contractAddress, "echo",
            TupleType<AddressType, BytesType, UInt32Type, VecType<EnumType<CallFlag>>>.GetByteStringFrom(
                AddressType.From(contractAddress.ToByteArray()),
                BytesType.From([0, 0, 0, 0]),
                UInt32Type.From(Voyager),
                VecType<EnumType<CallFlag>>.From(new[]
                {
                    EnumType<CallFlag>.From(CallFlag.AllowReentry),
                    EnumType<CallFlag>.From(CallFlag.TailCall),
                }.ToArray())
            ));

        var result = UInt32Type.From(queryResult.ToByteArray()).Value;
        result.ShouldBe(Voyager);

        return contractAddress;
    }

    [Fact(DisplayName = "works with the reentry and clone input flags.", Skip = "Not supported yet.")]
    public async Task<Address> TestReentryAndCloneInputFlags()
    {
        var contractAddress = await DeployContractAsync();

        var queryResult = await QueryAsync(contractAddress, "echo",
            TupleType<AddressType, BytesType, UInt32Type, VecType<EnumType<CallFlag>>>.GetByteStringFrom(
                AddressType.From(contractAddress.ToByteArray()),
                BytesType.From([0, 0, 0, 0]),
                UInt32Type.From(Voyager),
                VecType<EnumType<CallFlag>>.From(new[]
                {
                    EnumType<CallFlag>.From(CallFlag.AllowReentry),
                    EnumType<CallFlag>.From(CallFlag.CloneInput)
                }.ToArray())
            ));

        var result = UInt32Type.From(queryResult.ToByteArray()).Value;
        result.ShouldBe(Voyager);
        
        return contractAddress;
    }

    [Fact(DisplayName = "works with the reentry, tail call and clone input flags.", Skip = "Not supported yet.")]
    public async Task<Address> TestMultipleFlags()
    {
        var contractAddress = await DeployContractAsync();

        var foo = BytesType.From([0, 0, 0, 0]);
        var queryResult = await QueryAsync(contractAddress, "echo",
            TupleType<AddressType, BytesType, UInt32Type, VecType<EnumType<CallFlag>>>.GetByteStringFrom(
                AddressType.From(contractAddress.ToByteArray()),
                foo,
                UInt32Type.From(Voyager),
                VecType<EnumType<CallFlag>>.From(new[]
                {
                    EnumType<CallFlag>.From(CallFlag.AllowReentry),
                    EnumType<CallFlag>.From(CallFlag.TailCall),
                    EnumType<CallFlag>.From(CallFlag.CloneInput)
                }.ToArray())
            ));

        var result = UInt32Type.From(queryResult.ToByteArray()).Value;
        result.ShouldBe(Voyager);

        return contractAddress;
    }

    [Fact(DisplayName = "fails without the reentry flag.")]
    public async Task<Address> TestWithoutReentryFlag()
    {
        var contractAddress = await DeployContractAsync();

        var queryResult = await QueryWithExceptionAsync(contractAddress, "echo",
            TupleType<AddressType, BytesType, UInt32Type, VecType<EnumType<CallFlag>>>.GetByteStringFrom(
                AddressType.From(contractAddress.ToByteArray()),
                BytesType.From([0, 0, 0, 0]),
                UInt32Type.From(Voyager),
                VecType<EnumType<CallFlag>>.From(new[]
                {
                    EnumType<CallFlag>.From(CallFlag.TailCall)
                }.ToArray())
            ));

        var result = UInt32Type.From(queryResult.ToByteArray()).Value;
        result.ShouldBe(0u);

        return contractAddress;
    }
}