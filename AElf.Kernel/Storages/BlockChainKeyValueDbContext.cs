using AElf.Database;
using Volo.Abp.Data;

namespace AElf.Kernel.Storages
{
    [ConnectionStringName("blockchain")]
    public class BlockChainKeyValueDbContext : KeyValueDbContext<BlockChainKeyValueDbContext>
    {
        
    }

    [ConnectionStringName("state")]
    public class StateKeyValueDbContext : KeyValueDbContext<StateKeyValueDbContext>
    {
        
    }
}