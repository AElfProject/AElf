using System.Threading.Tasks;
using AElf.Management.Services;
using Xunit;

namespace AElf.Management.Tests
{
    public class WorkerServiceTest
    {
        [Fact(Skip = "require aws account")]
        //[Fact]
        public async Task ModifyWorkerCountTest()
        {
            var service = new WorkerService();
            await service.ModifyWorkerCount("default", 2);
        }
    }
}