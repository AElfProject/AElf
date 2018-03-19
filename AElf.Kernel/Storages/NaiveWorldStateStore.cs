using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.Kernel.Storages
{
    public class NaiveWorldStateStore : IWorldStateStore
    {
        private List<IWorldState> _worldStates = new List<IWorldState>();
        
        /// <inheritdoc />
        public Task SetWorldStateAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task<IWorldState> GetAsync(long height)
        {
            throw new NotImplementedException();
        }

        public Task<IWorldState> GetAsync()
        {
            throw new NotImplementedException();
        }
    }
}