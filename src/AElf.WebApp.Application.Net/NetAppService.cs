using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network.Application;
using AElf.OS.Network.Types;
using AElf.WebApp.Application.Net.Dto;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.ObjectMapping;

namespace AElf.WebApp.Application.Net;

public interface INetAppService
{
    Task<bool> AddPeerAsync(AddPeerInput input);

    Task<bool> RemovePeerAsync(string address);

    List<PeerDto> GetPeers(bool withMetrics);

    Task<GetNetworkInfoOutput> GetNetworkInfoAsync();
}

[Ump]
public class NetAppService : AElfAppService, INetAppService
{
    private static readonly string Version =
        typeof(NetApplicationWebAppAElfModule).Assembly.GetName().Version.ToString();

    private readonly INetworkService _networkService;
    private readonly IObjectMapper<NetApplicationWebAppAElfModule> _objectMapper;
    private readonly IReconnectionService _reconnectionService;

    public NetAppService(INetworkService networkService, IReconnectionService reconnectionService,
        IObjectMapper<NetApplicationWebAppAElfModule> objectMapper)
    {
        _networkService = networkService;
        _reconnectionService = reconnectionService;
        _objectMapper = objectMapper;
    }

    /// <summary>
    ///     Attempts to add a node to the connected network nodes
    /// </summary>
    /// <returns>true/false</returns>
    [Authorize]
    public async Task<bool> AddPeerAsync(AddPeerInput input)
    {
        return await _networkService.AddTrustedPeerAsync(input.Address);
    }

    /// <summary>
    ///     Attempts to remove a node from the connected network nodes
    /// </summary>
    /// <param name="address">ip address</param>
    /// <returns></returns>
    [Authorize]
    public async Task<bool> RemovePeerAsync(string address)
    {
        _reconnectionService.CancelReconnection(address);
        return await _networkService.RemovePeerByEndpointAsync(address, int.MaxValue);
    }

    /// <summary>
    ///     Get peer info about the connected network nodes
    /// </summary>
    /// <returns></returns>
    public List<PeerDto> GetPeers(bool withMetrics = false)
    {
        var peerList = _networkService.GetPeers();

        return _objectMapper.GetMapper().Map<List<PeerInfo>, List<PeerDto>>(peerList,
            opt => opt.Items[PeerInfoProfile.WithMetrics] = withMetrics);
    }

    /// <summary>
    ///     Get information about the node’s connection to the network.
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