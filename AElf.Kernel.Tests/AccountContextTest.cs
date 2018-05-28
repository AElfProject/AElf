using System.Threading.Tasks;
using AElf.Kernel.Services;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class AccountContextTest
    {
        private readonly AccountContextService _accountContextService;

        public AccountContextTest(AccountContextService accountContextService)
        {
            _accountContextService = accountContextService;
        }

        [Fact]
        public async Task GetAccountContextTest()
        {
            var chainId = Hash.Generate();
            var accountId = Hash.Generate();

            var context1 =  await _accountContextService.GetAccountDataContext(accountId, chainId);
            var context2 =  await _accountContextService.GetAccountDataContext(accountId, chainId);
            Assert.Equal(context1.IncrementId, context2.IncrementId);
            
        }

        [Fact]
        public async Task SetAccountContextTest()
        {
            var chainId = Hash.Generate();
            var accountId = Hash.Generate();

            var context1 =  await _accountContextService.GetAccountDataContext(accountId, chainId);
            context1.IncrementId++;
            await _accountContextService.SetAccountContext(context1);
            
            var context2 =  await _accountContextService.GetAccountDataContext(accountId, chainId);
            Assert.Equal((ulong)1, context2.IncrementId);
        }
        
    }
}