using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Configuration
{
    /// <summary>
    /// To control the limitation of count of txs fetching from tx hub each time.
    /// </summary>
    public class BlockTransactionLimitConfigurationNameProvider : IConfigurationNameProvider, ISingletonDependency
    {
        public static readonly string Name = "BlockTransactionLimit";
        public string ConfigurationName => Name;
    }
}