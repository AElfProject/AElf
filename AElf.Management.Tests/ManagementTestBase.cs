using AElf.TestBase;

namespace AElf.Management.Tests
{
    public class ManagementTestBase: AElfIntegratedTest<ManagementTestAElfModule>
    {
        public new T GetRequiredService<T>()
        {
            return base.GetRequiredService<T>();
        }
    }
}