using System.Threading.Tasks;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class TransactionManagerTest
    {
        private ITransactionManager _manager;

        public TransactionManagerTest(ITransactionManager manager)
        {
            _manager = manager;
        }

        [Fact]
        public async Task TestInsert()
        {
            await _manager.AddTransactionAsync(new Transaction());
        }
    }
}