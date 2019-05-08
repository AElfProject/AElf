using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network.Application;
using AElf.WebApp.Application.Net.Dto;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Net
{
    public interface INetAppService : IApplicationService
    {
        Task<bool> AddPeer(AddPeerInput input);

        Task<bool> RemovePeer(string address);

        List<PeerDto> GetPeers();

        Task<GetNetworkInfoOutput> GetNetworkInfo();
    }
    
    public class NetAppService : INetAppService
    {
        private readonly INetworkService _networkService;

        private static readonly string Version = typeof(NetApplicationWebAppAElfModule).Assembly.GetName().Version.ToString();

        public NetAppService(INetworkService networkService)
        {
            _networkService = networkService;
        }
        
        /// <summary>
        /// Attempts to add a node to the connected network nodes
        /// </summary>
        /// <returns>true/false</returns>
        public async Task<bool> AddPeer(AddPeerInput input)
        {
            return await _networkService.AddPeerAsync(input.Address);
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
        /// Get peer info about the connected network nodes
        /// </summary>
        /// <returns></returns>
        public List<PeerDto> GetPeers()
        {
            var peerList = _networkService.GetPeers();
            var peerDtoList = peerList.Select(p => new PeerDto
            {
                IpAddress = p.PeerIpAddress,
                ProtocolVersion = p.ProtocolVersion,
                ConnectionTime = p.ConnectionTime,
                Inbound = p.Inbound,
                StartHeight = p.StartHeight
            }).ToList();
            return peerDtoList;
        }

        /// <summary>
        /// Get information about the node’s connection to the network. 
        /// </summary>
        /// <returns></returns>
        public Task<GetNetworkInfoOutput> GetNetworkInfo()
        {
            var output = new GetNetworkInfoOutput
            {
                ProtocolVersion = KernelConstants.ProtocolVersion,
                Version = Version,
                Connections = _networkService.GetPeerIpList().Count
            };
            return Task.FromResult(output);
        }
    }
}