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
        
        /// <summary>
        /// Attempts to add a node to the connected network nodes
        /// </summary>
        /// <param name="address">ip address</param>
        /// <returns>true/false</returns>
        public async Task<bool> AddPeer(string address)
        {
            return await _networkService.AddPeerAsync(address);
        }
        
        /// <summary>
        /// Attempts to remove a node from the connected network nodes
        /// </summary>
        /// <param name="address">ip address</param>
        /// <returns></returns>
        public async Task<bool> RemovePeer(string address)
        {
            return await _networkService.RemovePeerAsync(address);
        }
        
        /// <summary>
        /// Get ip addresses about the connected network nodes
        /// </summary>
        /// <returns></returns>
        public Task<List<string>> GetPeers()
        {
            return Task.FromResult(_networkService.GetPeerIpList());
        }
    }
}