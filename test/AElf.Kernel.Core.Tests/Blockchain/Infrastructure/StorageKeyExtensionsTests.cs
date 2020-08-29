using AElf.Kernel.Infrastructure;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Blockchain.Infrastructure
{
    public class StorageKeyExtensionsTests
    {
        [Fact]
        public void ToStorageKeyTest()
        {
            var intNum = 1;
            long longNum = intNum;
            ulong ulongNum = 1;
            var numString = "1";
            longNum.ToStorageKey().ShouldBe(numString);
            intNum.ToStorageKey().ShouldBe(numString);
            ulongNum.ToStorageKey().ShouldBe(numString);
            var numToHash = HashHelper.ComputeFrom(intNum);
            numToHash.ToStorageKey().ShouldBe(numToHash.Value.ToBase64());
            var byteString = numToHash.ToByteString();
            byteString.ToStorageKey().ShouldBe(byteString.ToBase64());
            var address = SampleAddress.AddressList[0];
            address.ToStorageKey().ShouldBe(address.ToBase58());
        }
    }
}