using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Merkle;
using AElf.Kernel.Storages;

namespace AElf.Kernel
{
    public class WorldState : IWorldState
    {
        private readonly List<IAccountDataProvider> _accountDataProviders;

        
        public WorldState(List<IAccountDataProvider> accountDataProviders)
        {
            _accountDataProviders = accountDataProviders;
        }
        
        public Task<IHash> GetWorldStateMerkleTreeRootAsync()
        {
            throw new NotImplementedException();
        }
    }
}
