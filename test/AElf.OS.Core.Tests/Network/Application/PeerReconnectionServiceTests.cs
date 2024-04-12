using System.Linq;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.OS.Network.Infrastructure;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Application;

public class PeerReconnectionServiceTests : OSCoreNetworkServiceTestBase
{
    private readonly IPeerReconnectionStateProvider _connectionStateProvider;
    private readonly IReconnectionService _reconnectionService;

    public PeerReconnectionServiceTests()
    {
        _reconnectionService = GetRequiredService<IReconnectionService>();
        _connectionStateProvider = GetRequiredService<IPeerReconnectionStateProvider>();
    }

    [Fact]
    public void GetPeersReadyForReconnection_WithNull_ReturnsAll()
    {
        var endpoint = "127.0.0.1:5677";
        var endpointBeforeNow = "127.0.0.1:5678";
        var endpointAfterNow = "127.0.0.1:5679";

        _reconnectionService.SchedulePeerForReconnection(endpoint);
        _reconnectionService.SchedulePeerForReconnection(endpointBeforeNow);
        _reconnectionService.SchedulePeerForReconnection(endpointAfterNow);

        var peers = _connectionStateProvider.GetPeersReadyForReconnection(null);

        peers.Count.ShouldBe(3);
    }

    [Fact]
    public void ScheduledPeer_CannotAddPeerTwice()
    {
        var endpoint = "127.0.0.1:5677";

        _reconnectionService.SchedulePeerForReconnection(endpoint).ShouldBeTrue();
        _reconnectionService.SchedulePeerForReconnection(endpoint).ShouldBeFalse();

        var peers = _connectionStateProvider.GetPeersReadyForReconnection(null);

        peers.Count.ShouldBe(1);
        peers.First().Endpoint.ShouldBe(endpoint);
    }

    [Fact]
    public void ScheduledPeer_ShouldBeRetrievableWithTheProvider()
    {
        var endpoint = "127.0.0.1:5677";

        _reconnectionService.SchedulePeerForReconnection(endpoint);
        var peers = _connectionStateProvider.GetPeersReadyForReconnection(TimestampHelper.GetUtcNow().AddMinutes(2));

        peers.Count.ShouldBe(1);
        peers.First().Endpoint.ShouldBe(endpoint);
    }

    [Fact]
    public void RemovePeer_ShouldNotRetrievableWithTheProvider()
    {
        var endpoint = "127.0.0.1:5677";

        _reconnectionService.SchedulePeerForReconnection(endpoint);
        _reconnectionService.CancelReconnection(endpoint);

        var peers = _connectionStateProvider.GetPeersReadyForReconnection(TimestampHelper.GetUtcNow().AddSeconds(1));
        peers.Count.ShouldBe(0);
    }

    [Fact]
    public void CancelReconnection_NotExitEndpoint_ReturnFalse()
    {
        var result = _reconnectionService.CancelReconnection("127.0.0.1:5677");
        result.ShouldBeFalse();
    }

    [Fact]
    public void GetReconnectingPeer_Test()
    {
        var endpoint = "127.0.0.1:5677";
        _reconnectionService.SchedulePeerForReconnection(endpoint);
        var result = _reconnectionService.GetReconnectingPeer(endpoint);
        result.ShouldNotBeNull();
        result.Endpoint.ShouldBe(endpoint);
    }
}