using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.OS.Network.Application;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Net
{
    public interface INetAppService : IApplicationService
    {
        Task<bool> AddPeer(string address);

        Task<bool> RemovePeer(string address);

        Task<List<string>> GetPeers();
    }
    
    public class NetAppService : INetAppService
    {
        private readonly INetworkService _networkService;

        public NetAppService(INetworkService networkService)
        {
            _networkService = networkService;
        }
        
        public async Task<bool> AddPeer(string address)
        {
            return await _networkService.AddPeerAsync(address);
        }
        
        public async Task<bool> RemovePeer(string address)
        {
            return await _networkService.RemovePeerAsync(address);
        }
        
        public Task<List<string>> GetPeers()
        {
            return Task.FromResult(_networkService.GetPeerIpList());
        }
    }
}