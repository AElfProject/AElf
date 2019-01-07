using AElf.Database;
using Volo.Abp.Data;

namespace AElf.Kernel.Storages
{
    [ConnectionStringName("BlockchainDb")]
    public class BlockChainKeyValueDbContext : KeyValueDbContext<BlockChainKeyValueDbContext>
    {
        
    }

    [ConnectionStringName("StateDb")]
    public class StateKeyValueDbContext : KeyValueDbContext<StateKeyValueDbContext>
    {
        
    }
}