using AElf.OS.Node.Application;
using AElf.TestBase;
using Shouldly;
using Xunit;

namespace AElf.OS.Application
{
    public class NodeEnvironmentServiceTests : AElfIntegratedTest<OSCoreTestAElfModule> 
    {
        private readonly INodeEnvironmentService _nodeEnvironmentService;
        public NodeEnvironmentServiceTests()
        {
            _nodeEnvironmentService = GetRequiredService<INodeEnvironmentService>();
        }
        [Fact]
        public void GetAppDataPath_Test()
        {
            var path = _nodeEnvironmentService.GetAppDataPath();
            path.ShouldNotBeNull();
            path.ShouldContain("aelf");
        }
    }
}