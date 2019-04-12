using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContractExecution
{
    public class MockResourceUsageDetectionServiceTests : SmartContractExecutionTestBase
    {
        private MockResourceUsageDetectionService _resourceUsageDetectionService;
        
        public MockResourceUsageDetectionServiceTests()
        {
            _resourceUsageDetectionService = new MockResourceUsageDetectionService();
        }

        [Fact]
        public async Task GetResources_Basic_Test()
        {
            var transaction = new Transaction
            {
                From = Address.Generate(),
                To = Address.Zero,
                MethodName = "Test",
                Params = ByteString.CopyFromUtf8("test")
            };
            var results = await _resourceUsageDetectionService.GetResources(transaction);
            results.ShouldNotBeNull();
            
            var enumerable = results.ToList();
            enumerable.Count().ShouldBe(1);
            enumerable[0].ShouldBe(transaction.From.GetFormatted());
        }
    }
}