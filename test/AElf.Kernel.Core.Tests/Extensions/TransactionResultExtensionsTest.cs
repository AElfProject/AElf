using System.Collections.Generic;

namespace AElf.Kernel;

[Trait("Category", AElfBlockchainModule)]
public class TransactionResultExtensionsTest : AElfKernelTestBase
{
    [Fact]
    public void UpdateBloom_Test()
    {
        var transactionResult = new TransactionResult();

        transactionResult.UpdateBloom();
        transactionResult.Bloom.ShouldBe(ByteString.Empty);

        var logEvent1 = new LogEvent
        {
            Address = SampleAddress.AddressList[0],
            Name = "LogEvent1"
        };
        var logEvent2 = new LogEvent
        {
            Address = SampleAddress.AddressList[1],
            Name = "LogEvent2"
        };
        transactionResult.Logs.Add(new[] { logEvent1, logEvent2 });
        transactionResult.UpdateBloom();
        var bloom = new Bloom();
        bloom.Combine(new List<Bloom> { logEvent1.GetBloom(), logEvent2.GetBloom() });
        transactionResult.Bloom.ShouldBe(ByteString.CopyFrom(bloom.Data));
    }
}