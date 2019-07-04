using Volo.Abp.Modularity;

namespace AElf.Modularity
{
    public abstract class AElfModule : AbpModule
    {
    }

    public abstract class AElfModule<TSelf> : AElfModule
        where TSelf : AElfModule<TSelf>
    {
    }
}