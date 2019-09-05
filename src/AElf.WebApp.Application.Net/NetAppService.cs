using System;
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
        Task<bool> AddPeerAsync(AddPeerInput input);

        Task<bool> RemovePeerAsync(string address);

        List<PeerDto> GetPeers(bool withMetrics);

        Task<GetNetworkInfoOutput> GetNetworkInfoAsync();
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
        public async Task<bool> AddPeerAsync(AddPeerInput input)
        {
            return await _networkService.AddPeerAsync(input.Address);
        }
        
        /// <summary>
        /// Attempts to remove a node from the connected network nodes
        /// </summary>
        /// <param name="address">ip address</param>
        /// <returns></returns>
        public async Task<bool> RemovePeerAsync(string address)
        {
            return await _networkService.RemovePeerAsync(address);
        }
        
        /// <summary>
        /// Get peer info about the connected network nodes
        /// </summary>
        /// <returns></returns>
        public List<PeerDto> GetPeers(bool withMetrics = false)
        {
            var peerList = _networkService.GetPeers();
            
            var peerDtoList = peerList.Select(p => new PeerDto
            {
                IpAddress = p.RemoteEndpoint.ToString(),
                ProtocolVersion = p.Info.ProtocolVersion,
                ConnectionTime = p.Info.ConnectionTime,
                Inbound = p.Info.IsInbound,
                BufferedAnnouncementsCount = p.BufferedAnnouncementsCount,
                BufferedBlocksCount = p.BufferedBlocksCount,
                BufferedTransactionsCount = p.BufferedTransactionsCount,
                RequestMetrics = withMetrics ? p.GetRequestMetrics().Values.SelectMany(kvp => kvp).ToList() : null
            }).ToList();
            
            return peerDtoList;
        }

        /// <summary>
        /// Get information about the node’s connection to the network. 
        /// </summary>
        /// <returns></returns>
        public Task<GetNetworkInfoOutput> GetNetworkInfoAsync()
        {
            var output = new GetNetworkInfoOutput
            {
                ProtocolVersion = KernelConstants.ProtocolVersion,
                Version = Version,
                Connections = _networkService.GetPeers().Count
            };
            return Task.FromResult(output);
        }
    }
}