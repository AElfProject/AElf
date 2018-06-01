using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Services;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class ChainCreationServiceTests
    {
        private IChainCreationService _service;

        public ChainCreationServiceTests(IChainCreationService service)
        {
            _service = service;
        }

        [Fact]
        public async Task Test()
        {
            // TODO: *** Contract Issues ***
            await _service.CreateNewChainAsync("Hello".CalculateHash(), null);
        }
    }
}