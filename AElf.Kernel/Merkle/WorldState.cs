using System;
using System.Threading.Tasks;

namespace AElf.Kernel.Merkle
{
    public class WorldState : IWorldState
    {
        public IAccountDataProvider GetAccountDataProviderByAccount(IAccount account)
        {
            return DataBase.GetAccountDataProviderByAccount(account);
        }

        public Task<IHash<IMerkleTree<IHash>>> GetWorldStateMerkleTreeRootAsync()
        {
            throw new NotImplementedException();
        }
    }
}
