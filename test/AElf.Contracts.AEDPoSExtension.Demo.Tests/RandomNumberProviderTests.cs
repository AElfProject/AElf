using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.TestContract.RandomNumberProvider;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.AEDPoSExtension.Demo.Tests
{
    public class RandomNumberProviderTests : AEDPoSExtensionDemoTestBase
    {
        [Fact]
        public async Task GetRandomBytesTest_Hash()
        {
            var stub = await DeployRandomNumberProviderContract();
            var randomBytes = await stub.GetRandomBytes.CallAsync(new GetRandomBytesInput
            {
                Kind = 1,
                Value = HashHelper.ComputeFrom("Test1").ToByteString()
            }.ToBytesValue());
            var randomHash = new Hash();
            randomHash.MergeFrom(randomBytes.Value);
            randomHash.ShouldNotBeNull();
        }

        [Fact]
        public async Task GetRandomBytesTest_Int64()
        {
            var stub = await DeployRandomNumberProviderContract();

            var randomIntegers = new List<long>();

            for (var i = 0; i < 5; i++)
            {
                var randomBytes = await stub.GetRandomBytes.CallAsync(new GetRandomBytesInput
                {
                    Kind = 2,
                    Value = HashHelper.ComputeFrom("Test2").ToByteString()
                }.ToBytesValue());
                var randomNumber = new Int64Value();
                randomNumber.MergeFrom(randomBytes.Value);
                randomNumber.Value.ShouldBePositive();
                randomNumber.Value.ShouldBeLessThan(10000);
                await stub.GetRandomBytes.SendAsync(new GetRandomBytesInput
                {
                    Kind = 2,
                    Value = HashHelper.ComputeFrom("Test2").ToByteString()
                }.ToBytesValue());

                randomIntegers.Add(randomNumber.Value);
            }

            randomIntegers.Count.ShouldBe(5);
        }
    }
}