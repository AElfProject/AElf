using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network
{
    public class NetworkService : INetworkService, ISingletonDependency
    {
        private INetworkManager _networkManager;

        public NetworkService(INetworkManager networkManager)
        {
            _networkManager = networkManager;
        }
        
        public async Task Start()
        {
            await _networkManager.StartAsync();
        }

        public async Task Stop()
        {
            await _networkManager.StopAsync();
        }

        public void AddPeer(string address)
        {
            _networkManager.AddPeer(address);
        }

        public Task RemovePeer(string address)
        {
            return Task.FromResult(_networkManager.RemovePeer(address));
        }

        public List<string> GetPeers()
        {
            return _networkManager.GetPeers();
        }

        public async Task<IBlock> GetBlockByHash(Hash hash)
        {
            return await Task.FromResult<IBlock>(new Block());
        }
        
//        public Task<List<IPeer>> GetPeers() //todo
//        {
//            return Task.FromResult<List<IPeer>>(new List<IPeer>());
//        }
    }
}