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
        public void GetAccountContextTest()
        {
            var chainId = Hash.Generate();
            var accountId = Hash.Generate();

            var context1 = _accountContextService.GetAccountDataContext(accountId, chainId);
            var context2 = _accountContextService.GetAccountDataContext(accountId, chainId);
            Assert.Equal(context1, context2);
            
            context1.IncreasementId++;
            var context3 = _accountContextService.GetAccountDataContext(accountId, chainId);
            Assert.Equal(context1, context3);
            Assert.Equal(context3.IncreasementId, (ulong)1);
        }
        
    }
}