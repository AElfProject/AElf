using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Network;
using AElf.Network.Peers;
using Volo.Abp.DependencyInjection;

namespace AElf.Node.Protocol
{
    public class NetworkService : INetworkService, ISingletonDependency
    {
        private INetworkManager NetworkManager;
        
        public Task Start()
        {
            throw new NotImplementedException();
        }

        public Task Stop()
        {
            throw new NotImplementedException();
        }

        public Task<IBlock> GetBlockByHash(Hash hash)
        {
            return Task.FromResult<IBlock>(new Block());
        }
        
        public Task<List<IPeer>> GetPeers()
        {
            return Task.FromResult<List<IPeer>>(new List<IPeer>());
        }
    }
}