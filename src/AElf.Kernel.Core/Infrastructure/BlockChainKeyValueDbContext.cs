using AElf.Database;
using Volo.Abp.Data;

namespace AElf.Kernel.Infrastructure;

[ConnectionStringName("BlockchainDb")]
public class BlockchainKeyValueDbContext : KeyValueDbContext<BlockchainKeyValueDbContext>
{
}

[ConnectionStringName("StateDb")]
public class StateKeyValueDbContext : KeyValueDbContext<StateKeyValueDbContext>
{
}