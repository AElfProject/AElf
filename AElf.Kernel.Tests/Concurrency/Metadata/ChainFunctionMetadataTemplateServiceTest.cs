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
            cfts.TryAddNewFunctionMetadataFromContractType(typeof(SimpleTokenContract));
            cfts.TryAddNewFunctionMetadataFromContractType(typeof(Casino));

        }
    }
}