namespace AElf.Kernel;

[Trait("Category", AElfBlockchainModule)]
public class BlockStateSetExtensionTest : AElfKernelTestBase
{
    [Fact]
    public void TryGetState_Test()
    {
        var blockStateSet = new BlockStateSet();

        blockStateSet.TryGetState("key1", out var value).ShouldBeFalse();

        blockStateSet.Deletes.Add("key1");
        blockStateSet.TryGetState("key1", out value).ShouldBeTrue();
        value.ShouldBeNull();

        blockStateSet.Changes.Add("key1", ByteString.CopyFromUtf8("key1"));
        blockStateSet.TryGetState("key1", out value).ShouldBeTrue();
        value.ShouldBeNull();

        blockStateSet.Changes.Add("key2", ByteString.CopyFromUtf8("key2"));
        blockStateSet.TryGetState("key2", out value).ShouldBeTrue();
        value.ShouldBe(ByteString.CopyFromUtf8("key2"));
    }

    [Fact]
    public void TryGetExecutedCache_Test()
    {
        var blockStateSet = new BlockStateSet();
        blockStateSet.TryGetExecutedCache("key1", out var value).ShouldBeFalse();

        blockStateSet.BlockExecutedData.Add("key1", ByteString.CopyFromUtf8("key1"));
        blockStateSet.TryGetExecutedCache("key1", out value).ShouldBeTrue();
        value.ShouldBe(ByteString.CopyFromUtf8("key1"));
    }
}