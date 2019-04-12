using System.Threading.Tasks;
using AElf.Management.Interfaces;
using AElf.Management.Services;
using Xunit;

namespace AElf.Management.Tests
{
    public class WorkerServiceTest : ManagementTestBase
    {
        private readonly IWorkerService _workerService;
        public WorkerServiceTest()
        {
            _workerService = GetRequiredService<IWorkerService>();
        }

        [Fact(Skip = "require aws account")]
        //[Fact]
        public async Task ModifyWorkerCountTest()
        {
            await _workerService.ModifyWorkerCount("default", 2);
        }
    }
}