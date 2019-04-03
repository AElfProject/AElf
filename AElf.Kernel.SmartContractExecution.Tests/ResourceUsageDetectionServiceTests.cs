using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContractExecution
{
    public class ResourceUsageDetectionServiceTests : FunctionMetadataTestBase
    {
        private IResourceUsageDetectionService _resourceUsageDetectionService;

        public ResourceUsageDetectionServiceTests()
        {
            _resourceUsageDetectionService = GetRequiredService<IResourceUsageDetectionService>();
        }

        [Fact]
        public async Task GetResource_Test()
        {
            var transaction = new Transaction
            {
                Fee = 1,
                From = Address.Generate(),
                To = Address.Generate(),
                Params = ByteString.CopyFromUtf8("test"),
                MethodName = "TestMethod"
            };
            var result = await _resourceUsageDetectionService.GetResources(transaction);
            result.Count().ShouldBe(2);
            result.Contains($"test1.{transaction.From.GetFormatted()}").ShouldBeTrue();
            result.Contains("test2");
        }
    }
}