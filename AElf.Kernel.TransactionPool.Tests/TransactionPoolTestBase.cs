using AElf.TestBase;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool.Tests
{
    public class TransactionPoolTestBase:AElfIntegratedTest<TransactionPoolAElfModule>
    {
        public new T GetRequiredService<T>()
        {
            return base.GetRequiredService<T>();
        }
    }
}