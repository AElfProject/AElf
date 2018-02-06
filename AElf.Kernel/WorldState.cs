using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public class WorldState : IWorldState
    {
        public IAccountDataProvider GetAccountDataProviderByAccount(IAccount account)
        {
            throw new NotImplementedException();
        }

        public Task<IHash<IMerkleTree<IHash>>> GetWorldStateMerkleTreeRootAsync()
        {
            throw new NotImplementedException();
        }
    }
}