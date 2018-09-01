using AElf.Management.Services;
using Xunit;

namespace AElf.Management.Tests
{
    public class WorkerServiceTest
    {
        [Fact]
        public void TestModifyWorkerCount()
        {
            var service = new WorkerService();

            service.ModifyWorkerCount("default", 2);
        }
    }
}