using AElf.Kernel.Concurrency.Metadata;
using AElf.Contracts.Examples;
using Xunit;

namespace AElf.Kernel.Tests.Concurrency.Metadata
{
    public class ChainFunctionMetadataTemplateServiceTest
    {
        [Fact]
        public void TestMetadataExtraction()
        {
            ChainFunctionMetadataTemplateService cfts = new ChainFunctionMetadataTemplateService();
            cfts.TryAddNewContract(typeof(SimpleTokenContract));
            cfts.TryAddNewContract(typeof(Casino));

        }
    }
}