using System.Threading.Tasks;
using AElf.Kernel.Managers;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class TransactionManagerTests
    {
        private ITransactionManager _manager;

        public TransactionManagerTests(ITransactionManager manager)
        {
            _manager = manager;
        }

        [Fact]
        public async Task TestInsert()
        {
            await _manager.AddTransactionAsync(new Transaction
            {
                From = Hash.Generate(),
                To = Hash.Generate()
            });
        }
    }
}