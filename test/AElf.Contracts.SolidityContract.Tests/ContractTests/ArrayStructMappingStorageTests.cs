using System.Threading.Tasks;
using Scale;
using Shouldly;
using Xunit.Abstractions;

namespace AElf.Contracts.SolidityContract;

public sealed class ArrayStructMappingStorageTests : SolidityContractTestBase
{
    public ArrayStructMappingStorageTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        ContractPath = "contracts/array_struct_mapping_storage.contract";
    }

    [Fact(DisplayName = "Test setNumber function.")]
    public async Task SetNumberTest()
    {
        const int number = 2147483647;

        var contractAddress = await DeployContractAsync();

        // Execute setNumber function.
        await ExecuteTransactionAsync(contractAddress, "setNumber", Int64Type.GetByteStringFrom(number));

        // Read number property.
        var returnValue = await QueryAsync(contractAddress, "number");
        Int64Type.From(returnValue.ToByteArray()).Value.ShouldBe(number);
    }

    [Fact(DisplayName = "Test struct map.")]
    public async Task StructMapTest()
    {
        var contractAddress = await DeployContractAsync();

        // let's add two elements to our array
        await ExecuteTransactionAsync(contractAddress, "push");
        await ExecuteTransactionAsync(contractAddress, "push");

        // set some values
        for (var arrayNumber = 0; arrayNumber < 2; arrayNumber++)
        {
            for (var i = 0; i < 10; i++)
            {
                var index = 102 + i + arrayNumber * 500;
                var val = 300331 + i;
                await ExecuteTransactionAsync(contractAddress, "set",
                    TupleType<UInt64Type, UInt64Type, UInt64Type>.GetByteStringFrom(
                        UInt64Type.From((ulong)arrayNumber),
                        UInt64Type.From((ulong)index),
                        UInt64Type.From((ulong)val)
                    ));
            }
        }

        // test our values
        for (var arrayNumber = 0; arrayNumber < 2; arrayNumber++)
        {
            for (var i = 0; i < 10; i++)
            {
                var returnValue = await QueryAsync(contractAddress, "get",
                    TupleType<UInt64Type, UInt64Type>.GetByteStringFrom(
                        UInt64Type.From((ulong)arrayNumber),
                        UInt64Type.From((ulong)(102 + i + arrayNumber * 500))
                    ));
                var output = UInt64Type.From(returnValue.ToByteArray());
                output.Value.ShouldBe((ulong)(300331 + i));
            }
        }

        // delete one and try again
        await ExecuteTransactionAsync(contractAddress, "rm",
            TupleType<UInt64Type, UInt64Type>.GetByteStringFrom(
                UInt64Type.From(0),
                UInt64Type.From(104)
            ));
        for (var i = 0; i < 10; i++)
        {
            var returnValue = await QueryAsync(contractAddress, "get",
                TupleType<UInt64Type, UInt64Type>.GetByteStringFrom(
                    UInt64Type.From(0),
                    UInt64Type.From((ulong)(102 + i))
                ));
            var output = UInt64Type.From(returnValue.ToByteArray());
            if (i != 2)
            {
                output.Value.ShouldBe((ulong)(300331 + i));
            }
            else
            {
                output.Value.ShouldBe((ulong)0);
            }
        }
    }
}