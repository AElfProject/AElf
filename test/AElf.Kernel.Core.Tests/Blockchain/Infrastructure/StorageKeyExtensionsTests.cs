using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.Blockchain.Infrastructure;

[Trait("Category", AElfBlockchainModule)]
public class StorageKeyExtensionsTests : AElfKernelTestBase
{
    [Fact]
    public void ToStorageKeyTest()
    {
        const int intNum = 1;
        const long longNum = intNum;
        const ulong ulongNum = 1;
        const string numString = "1";
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